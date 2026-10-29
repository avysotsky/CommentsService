using System.Xml.Linq;
using CommentsService.Application.Abstractions.Persistence;
using CommentsService.Domain.Entities;
using Xunit;

namespace CommentsService.Domain.Tests;

public sealed class ArchitectureTests
{
    private static string RepositoryRoot()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
            if (File.Exists(Path.Combine(folder.FullName, "CommentsService.slnx"))) return folder.FullName;
        throw new DirectoryNotFoundException("Run architecture tests inside the source checkout.");
    }

    [Fact]
    public void ProjectDependenciesPointInward()
    {
        var root = RepositoryRoot();
        XDocument Project(string name) => XDocument.Load(Path.Combine(root, "src", name, name + ".csproj"));
        string[] References(string name) => Project(name).Descendants("ProjectReference")
            .Select(x => Path.GetFileNameWithoutExtension(x.Attribute("Include")!.Value)).Order().ToArray();
        Assert.Empty(References("CommentsService.Domain"));
        Assert.Empty(Project("CommentsService.Domain").Descendants("PackageReference"));
        Assert.Empty(Project("CommentsService.Domain").Descendants("FrameworkReference"));
        Assert.Equal(new[] { "CommentsService.Domain" }, References("CommentsService.Application"));
        Assert.Empty(Project("CommentsService.Application").Descendants("PackageReference"));
        Assert.Empty(Project("CommentsService.Application").Descendants("FrameworkReference"));
        Assert.Equal(new[] { "CommentsService.Application", "CommentsService.Domain" }, References("CommentsService.Infrastructure"));
        Assert.Equal(new[] { "CommentsService.Application", "CommentsService.Infrastructure" }, References("CommentsService.Api"));
        var packages = Project("CommentsService.Api").Descendants("PackageReference")
            .Select(x => x.Attribute("Include")!.Value).ToArray();
        Assert.All(packages, package => Assert.Equal("Microsoft.EntityFrameworkCore.Design", package));
    }

    [Fact]
    public void DomainAssemblyHasNoPersistenceOrHttpDependencies()
    {
        var references = typeof(Author).Assembly.GetReferencedAssemblies().Select(x => x.Name!).ToArray();
        Assert.DoesNotContain(references, x => x.StartsWith("Microsoft.EntityFrameworkCore") ||
            x.StartsWith("Npgsql") || x.StartsWith("Microsoft.AspNetCore") ||
            x.StartsWith("CommentsService.Infrastructure") || x.StartsWith("CommentsService.Api"));
    }

    [Fact]
    public void ApplicationAssemblyHasNoInfrastructureOrHttpDependencies()
    {
        var references = typeof(ICommentRepository).Assembly.GetReferencedAssemblies().Select(x => x.Name!).ToArray();
        Assert.DoesNotContain(references, x => x.StartsWith("Microsoft.EntityFrameworkCore") ||
            x.StartsWith("Npgsql") || x.StartsWith("Microsoft.AspNetCore") ||
            x.StartsWith("CommentsService.Infrastructure") || x.StartsWith("CommentsService.Api"));
    }

    [Fact]
    public void ApplicationPortsDoNotExposeProviderOrHttpTypes()
    {
        static void Check(Type type)
        {
            var name = type.FullName ?? type.Name;
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore", name);
            Assert.DoesNotContain("Npgsql", name);
            Assert.DoesNotContain("Microsoft.AspNetCore", name);
            Assert.False(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>));
            foreach (var argument in type.GenericTypeArguments) Check(argument);
        }
        foreach (var contract in typeof(ICommentRepository).Assembly.GetExportedTypes().Where(type => type.IsInterface))
        foreach (var method in contract.GetMethods())
        {
            Check(method.ReturnType);
            foreach (var parameter in method.GetParameters()) Check(parameter.ParameterType);
        }
    }

    [Fact]
    public void HttpEndpointsDoNotReferenceInfrastructureOrDomain()
    {
        var folder = Path.Combine(RepositoryRoot(), "src", "CommentsService.Api", "Endpoints");
        foreach (var file in Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            Assert.DoesNotContain("CommentsService.Infrastructure", source);
            Assert.DoesNotContain("CommentsService.Domain", source);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore", source);
            Assert.DoesNotContain("Npgsql", source);
        }
    }
}
