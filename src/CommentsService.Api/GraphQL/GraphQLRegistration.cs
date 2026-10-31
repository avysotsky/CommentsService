using CommentsService.Api.Configuration;
using HotChocolate.Execution.Configuration;

namespace CommentsService.Api.GraphQL;

public static class GraphQLRegistration
{
    public static void AddCommentGraphQL(this IServiceCollection services, RuntimeSettings runtime)
    {
        services.AddSingleton(runtime.GraphQL);
        services.AddSingleton<GraphQLCapacity>();
        services.AddGraphQLServer()
            .AddQueryType<CommentQuery>()
            .AddErrorFilter<CommentErrorFilter>()
            .AddMaxExecutionDepthRule(runtime.GraphQL.MaximumDepth, skipIntrospectionFields: false, allowRequestOverrides: false)
            .ModifyOptions(options =>
            {
                options.DefaultResolverStrategy = HotChocolate.Execution.ExecutionStrategy.Serial;
                options.OperationDocumentCacheSize = 64;
                options.PreparedOperationCacheSize = 64;
            })
            .ModifyParserOptions(options =>
            {
                options.MaxAllowedFields = runtime.GraphQL.MaximumFields;
                options.MaxAllowedRecursionDepth = 32;
                options.MaxAllowedNodes = 2048;
                options.MaxAllowedTokens = 4096;
            })
            .ModifyRequestOptions(options =>
            {
                options.IncludeExceptionDetails = false;
                options.ExecutionTimeout = TimeSpan.FromSeconds(runtime.GraphQL.ExecutionTimeoutSeconds);
            })
            .ModifyCostOptions(options =>
            {
                options.MaxFieldCost = runtime.GraphQL.MaximumFieldCost;
                options.MaxTypeCost = runtime.GraphQL.MaximumTypeCost;
                options.DefaultListSize = Math.Max(runtime.Limits.RootPageSize, runtime.Limits.MaximumListLimit);
                options.EnforceCostLimits = true;
                options.CostPlanCacheSize = 64;
            })
            .ModifyServerOptions(options =>
            {
                options.EnableGetRequests = false;
                options.EnableMultipartRequests = false;
                options.EnableSchemaRequests = false;
                options.Batching = HotChocolate.AspNetCore.AllowedBatching.None;
                options.Tool.Enable = false;
            });
    }
}
