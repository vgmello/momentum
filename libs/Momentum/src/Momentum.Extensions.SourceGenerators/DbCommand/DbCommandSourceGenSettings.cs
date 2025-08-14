// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.SourceGenerators.DbCommand;

/// <summary>
///     Contains global configuration settings for the DbCommand source generator, extracted from MSBuild properties.
/// </summary>
/// <param name="DbCommandDefaultParamCase">
///     The default parameter case conversion to apply when DbCommandAttribute.ParamsCase is set to Unset.
///     This provides a project-wide default for how property names are converted to database parameter names.
/// </param>
/// <param name="DbCommandParamPrefix">
///     A global prefix to add to all generated parameter names. This is applied after case conversion and
///     Column attribute overrides. Useful for database systems that require parameter prefixes.
/// </param>
/// <remarks>
/// <para><strong>MSBuild Integration:</strong></para>
/// <para>These settings are configured through MSBuild properties in the project file:</para>
/// <code>
/// &lt;Project Sdk="Microsoft.NET.Sdk"&gt;
///   &lt;PropertyGroup&gt;
///     &lt;DbCommandDefaultParamCase&gt;SnakeCase&lt;/DbCommandDefaultParamCase&gt;
///     &lt;DbCommandParamPrefix&gt;p_&lt;/DbCommandParamPrefix&gt;
///   &lt;/PropertyGroup&gt;
/// &lt;/Project&gt;
/// </code>
/// 
/// <para><strong>Parameter Name Generation Order:</strong></para>
/// <list type="number">
///   <item><strong>Base Name:</strong> Start with C# property name (e.g., "FirstName")</item>
///   <item><strong>Column Override:</strong> Apply [Column("custom")] if present → "custom"</item>
///   <item><strong>Case Conversion:</strong> Apply DbCommandDefaultParamCase if no Column attribute → "first_name"</item>
///   <item><strong>Global Prefix:</strong> Apply DbCommandParamPrefix → "p_first_name"</item>
/// </list>
/// 
/// <para><strong>Configuration Precedence:</strong></para>
/// <list type="bullet">
///   <item><strong>Highest:</strong> [Column("name")] attribute on individual properties</item>
///   <item><strong>Medium:</strong> DbCommandAttribute.ParamsCase on individual commands</item>
///   <item><strong>Lowest:</strong> Global MSBuild settings (DbCommandDefaultParamCase, DbCommandParamPrefix)</item>
/// </list>
/// 
/// <para><strong>Parameter Case Options:</strong></para>
/// <list type="bullet">
///   <item><strong>None:</strong> Use property names as-is (e.g., "FirstName" → "@FirstName")</item>
///   <item><strong>SnakeCase:</strong> Convert to snake_case (e.g., "FirstName" → "@first_name")</item>
/// </list>
/// 
/// <para><strong>Global Prefix Usage:</strong></para>
/// <para>The DbCommandParamPrefix is useful for:</para>
/// <list type="bullet">
///   <item><strong>Stored Procedure Conventions:</strong> Some teams prefix all SP parameters (e.g., "p_user_id")</item>
///   <item><strong>Avoiding Keywords:</strong> Prefix to avoid SQL reserved words (e.g., "p_order" instead of "order")</item>
///   <item><strong>Multi-Tenant Systems:</strong> Different prefixes for different tenant databases</item>
///   <item><strong>Legacy System Integration:</strong> Match existing database parameter naming conventions</item>
/// </list>
/// 
/// <para><strong>Build-Time Resolution:</strong></para>
/// <para>These settings are resolved during compilation from the MSBuild context, allowing different
/// configurations for different build environments (Development, Staging, Production).</para>
/// </remarks>
/// <example>
/// <para><strong>Example Configuration Effects:</strong></para>
/// <code>
/// // MSBuild configuration:
/// // &lt;DbCommandDefaultParamCase&gt;SnakeCase&lt;/DbCommandDefaultParamCase&gt;
/// // &lt;DbCommandParamPrefix&gt;sp_&lt;/DbCommandParamPrefix&gt;
/// 
/// [DbCommand(sp: "create_user")]
/// public record CreateUserCommand(
///     string FirstName,           // → @sp_first_name
///     string LastName,            // → @sp_last_name
///     [Column("email_addr")] string Email  // → @email_addr (Column overrides prefix)
/// ) : ICommand&lt;int&gt;;
/// 
/// // Generated ToDbParams():
/// public object ToDbParams()
/// {
///     var p = new 
///     {
///         sp_first_name = this.FirstName,
///         sp_last_name = this.LastName,
///         email_addr = this.Email
///     };
///     return p;
/// }
/// </code>
/// 
/// <para><strong>Environment-Specific Configuration:</strong></para>
/// <code>
/// &lt;!-- Development environment --&gt;
/// &lt;PropertyGroup Condition="'$(Configuration)' == 'Debug'"&gt;
///   &lt;DbCommandDefaultParamCase&gt;None&lt;/DbCommandDefaultParamCase&gt;
///   &lt;DbCommandParamPrefix&gt;&lt;/DbCommandParamPrefix&gt;
/// &lt;/PropertyGroup&gt;
/// 
/// &lt;!-- Production environment --&gt;
/// &lt;PropertyGroup Condition="'$(Configuration)' == 'Release'"&gt;
///   &lt;DbCommandDefaultParamCase&gt;SnakeCase&lt;/DbCommandDefaultParamCase&gt;
///   &lt;DbCommandParamPrefix&gt;prod_&lt;/DbCommandParamPrefix&gt;
/// &lt;/PropertyGroup&gt;
/// </code>
/// </example>
public record DbCommandSourceGenSettings(DbParamsCase DbCommandDefaultParamCase, string DbCommandParamPrefix);
