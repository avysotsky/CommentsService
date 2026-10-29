using System.Xml.Linq;
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
        Assert.Equal(new[] { "CommentsService.Domain" }, References("CommentsService.Infrastructure"));
        Assert.Equal(new[] { "CommentsService.Infrastructure" }, References("CommentsService.Api"));
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
}
