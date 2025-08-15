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
/// <!--@include: @code/source-generation/dbcommand-settings-detailed.md -->
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
