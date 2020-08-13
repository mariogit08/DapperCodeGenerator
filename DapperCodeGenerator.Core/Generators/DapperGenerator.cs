using System.Linq;
using System.Text;
using DapperCodeGenerator.Core.Extensions;
using DapperCodeGenerator.Core.Models;

namespace DapperCodeGenerator.Core.Generators
{
    public static class DapperGenerator
    {
        const string ConnectionStringPlaceholder = "ConnectionName";
        private static string ConnectionStringName = "ConnectionName";

        public static string GenerateDapperFromDatabase(Database database, string defaultNamespace = "DapperCodeGenerator.Repositories")
        {
            ConnectionStringName = $"{ConnectionStringPlaceholder}.{database.DatabaseName}";

            var stringBuilder = new StringBuilder();
            var publicClassStructure = GeneratePublicClass(database.DatabaseName);
            var namespaceStructure = GenerateNamespace(defaultNamespace);

            stringBuilder.AppendLine(namespaceStructure);
            stringBuilder.AppendLine(publicClassStructure);

            for (var i = 0; i < database.Tables.Count; i++)
            {
                stringBuilder.Append(GenerateDapperFromTable(database.Tables[i]));

                if (i < database.Tables.Count - 1)
                {
                    stringBuilder.AppendLine();
                }
            }

            stringBuilder.AppendLine($"{PadBy(1)}}}");
            stringBuilder.AppendLine("}");
            stringBuilder.AppendLine();

            return stringBuilder.ToString();
        }

        private static string GeneratePublicClass(string entityName)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"{PadBy(1)}public class {entityName}Repository");
            stringBuilder.AppendLine($"{PadBy(1)}{{");
            return stringBuilder.ToString();
        }

        private static string GenerateNamespace(string namespaceText)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"namespace {namespaceText}");
            stringBuilder.AppendLine("{");
            return stringBuilder.ToString();
        }

        public static string GenerateDapperFromTable(DatabaseTable table)
        {
            var stringBuilder = new StringBuilder();
            var publicClassStructure = GeneratePublicClass(table.TableName);

            stringBuilder.AppendLine(publicClassStructure);
            
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($@"{PadBy(1)}private readonly IDbConnectionFactory _dbConnectionFactory;");
            stringBuilder.AppendLine($@"{PadBy(1)}public {table.TableName}Repository(IDbConnectionFactory dbConnectionFactory)");
            stringBuilder.AppendLine($@"{PadBy(1)}{{");
            stringBuilder.AppendLine($@"{PadBy(1)}_dbConnectionFactory = dbConnectionFactory;");
            stringBuilder.AppendLine($@"{PadBy(1)}}}");

            GenerateDapperGetMethodsFromTable(stringBuilder, table);

            stringBuilder.AppendLine();
            GenerateDapperFindMethodsFromTable(stringBuilder, table);

            stringBuilder.AppendLine();
            GenerateDapperInsertMethodsFromTable(stringBuilder, table);

            stringBuilder.AppendLine();
            GenerateDapperUpdateMethodsFromTable(stringBuilder, table);

            stringBuilder.AppendLine();

            return stringBuilder.ToString();
        }

        private static void GenerateDapperGetMethodsFromTable(StringBuilder stringBuilder, DatabaseTable table)
        {
            var primaryKeyColumns = table.Columns.Where(tc => tc.IsPrimaryKey).ToList();
            if (primaryKeyColumns.Count > 0)
            {
                var methodParameters = primaryKeyColumns.GetMethodParameters();
                var sqlWhereClauses = primaryKeyColumns.GetSqlWhereClauses();
                var dapperProperties = primaryKeyColumns.GetDapperProperties();

                stringBuilder.AppendLine($"{PadBy(2)}public async Task<{table.DataModelName}> ObterPorId(int {table.DataModelName.FirstCharToLower()}Id)");
                stringBuilder.AppendLine($"{PadBy(2)}{{");
                stringBuilder.AppendLine($"{PadBy(3)}{GenerateUsingDBConectionFactory()}");
                stringBuilder.AppendLine($"{PadBy(3)}{{");
                stringBuilder.AppendLine($"{PadBy(4)}const string getQuery = \"SELECT * FROM {table.TableName} WHERE {sqlWhereClauses}\";");
                stringBuilder.AppendLine($"{PadBy(4)}return await  db.QuerySingleAsync<{table.DataModelName}>(getQuery, new {{ {dapperProperties} }});");
                stringBuilder.AppendLine($"{PadBy(3)}}}");
                stringBuilder.AppendLine($"{PadBy(2)}}}");
            }
            else
            {
                stringBuilder.AppendLine($"{PadBy(2)}// INFO: There are no primary keys for the Dapper code generation tool to generate get method(s).");
            }
        }

        private static string GenerateUsingDBConectionFactory()
        {
            return $"using (var db = _dbConnectionFactory.CreateConnection({ConnectionStringName}))";
        }

        private static void GenerateDapperFindMethodsFromTable(StringBuilder stringBuilder, DatabaseTable table)
        {
            stringBuilder.AppendLine($"{PadBy(2)}public async Task<IEnumerable<{table.DataModelName}>> BuscarTodos()");
            stringBuilder.AppendLine($"{PadBy(2)}{{");
            stringBuilder.AppendLine($"{PadBy(3)}{GenerateUsingDBConectionFactory()}");
            stringBuilder.AppendLine($"{PadBy(3)}{{");
            stringBuilder.AppendLine($"{PadBy(4)}const string findAllQuery = \"SELECT * FROM {table.TableName}\";");
            stringBuilder.AppendLine($"{PadBy(4)}var results = await db.QueryAsync<{table.DataModelName}>(findAllQuery);");
            stringBuilder.AppendLine($"{PadBy(4)}return results;");
            stringBuilder.AppendLine($"{PadBy(3)}}}");
            stringBuilder.AppendLine($"{PadBy(2)}}}");

            stringBuilder.AppendLine();

            var methodParameters = table.Columns.GetMethodParameters(parametersAreOptional: true);
            var sqlWhereClauses = table.Columns.GetSqlWhereClauses(parametersAreOptional: true);
            var dapperProperties = table.Columns.GetDapperProperties();

            stringBuilder.AppendLine($"{PadBy(2)}public async Task<IEnumerable<{table.DataModelName}>> FiltrarTodos({table.DataModelName} {table.DataModelName.FirstCharToLower()})");
            stringBuilder.AppendLine($"{PadBy(2)}{{");
            stringBuilder.AppendLine($"{PadBy(3)}{GenerateUsingDBConectionFactory()}");
            stringBuilder.AppendLine($"{PadBy(3)}{{");
            stringBuilder.AppendLine($"{PadBy(4)}const string findByAllQuery = \"SELECT * FROM {table.TableName} WHERE {sqlWhereClauses}\";");
            stringBuilder.AppendLine($"{PadBy(4)}var results = await db.QueryAsync<{table.DataModelName}>(findByAllQuery,{table.DataModelName.FirstCharToLower()});");
            stringBuilder.AppendLine($"{PadBy(4)}return results;");
            stringBuilder.AppendLine($"{PadBy(3)}}}");
            stringBuilder.AppendLine($"{PadBy(2)}}}");

            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"{PadBy(2)}public async Task<IEnumerable<{table.DataModelName}>> FiltrarQualquer({table.DataModelName} {table.DataModelName.FirstCharToLower()})");
            stringBuilder.AppendLine($"{PadBy(2)}{{");
            stringBuilder.AppendLine($"{PadBy(3)}{GenerateUsingDBConectionFactory()}");
            stringBuilder.AppendLine($"{PadBy(3)}{{");
            // TODO: string replace is not ideal, should separate cases better
            stringBuilder.AppendLine($"{PadBy(4)}const string findByAnyQuery = \"SELECT * FROM {table.TableName} WHERE {sqlWhereClauses.Replace(" AND ", " OR ").Replace("IS NULL OR", "IS NOT NULL AND")}\";");
            stringBuilder.AppendLine($"{PadBy(4)}var results = await db.QueryAsync<{table.DataModelName}>(findByAnyQuery, {table.DataModelName.FirstCharToLower()});");
            stringBuilder.AppendLine($"{PadBy(4)}return results;");
            stringBuilder.AppendLine($"{PadBy(3)}}}");
            stringBuilder.AppendLine($"{PadBy(2)}}}");
        }

        private static void GenerateDapperInsertMethodsFromTable(StringBuilder stringBuilder, DatabaseTable table)
        {
            var methodParameters = table.Columns.GetMethodParameters();
            var columnNames = table.Columns.GetColumnNames();
            var sqlInsertValues = table.Columns.GetSqlInsertValues();
            var dapperParameters = table.Columns.GetDapperProperties();

            stringBuilder.AppendLine($"{PadBy(2)}public async Task<int> Inserir({table.DataModelName} {table.DataModelName.FirstCharToLower()})");
            stringBuilder.AppendLine($"{PadBy(2)}{{");
            stringBuilder.AppendLine($"{PadBy(3)}{GenerateUsingDBConectionFactory()}");
            stringBuilder.AppendLine($"{PadBy(3)}{{");
            stringBuilder.AppendLine($"{PadBy(4)}const string insertQuery = \"INSERT INTO {table.TableName} ({columnNames}) VALUES ({sqlInsertValues})\";");
            stringBuilder.AppendLine($"{PadBy(4)}var rowsAffected = await db.ExecuteAsync(insertQuery, {table.DataModelName.FirstCharToLower()});");
            stringBuilder.AppendLine($"{PadBy(4)}return rowsAffected;");
            stringBuilder.AppendLine($"{PadBy(3)}}}");
            stringBuilder.AppendLine($"{PadBy(2)}}}");
        }

        private static void GenerateDapperUpdateMethodsFromTable(StringBuilder stringBuilder, DatabaseTable table)
        {
            var primaryKeyColumns = table.Columns.Where(tc => tc.IsPrimaryKey).ToList();
            if (primaryKeyColumns.Count > 0)
            {
                var methodParameters = table.Columns.GetMethodParameters();
                var columnNames = table.Columns.GetColumnNames();
                var sqlWhereClauses = table.Columns.GetSqlWhereClauses();

                var sqlWhereClausesForPrimaryKeys = primaryKeyColumns.GetSqlWhereClauses();
                var dapperParametersForPrimaryKeys = primaryKeyColumns.GetDapperProperties();

                stringBuilder.AppendLine($"{PadBy(2)}public async Task<int> Atualizar({table.DataModelName} {table.DataModelName.FirstCharToLower()})");
                stringBuilder.AppendLine($"{PadBy(2)}{{");
                stringBuilder.AppendLine($"{PadBy(3)}{GenerateUsingDBConectionFactory()}");
                stringBuilder.AppendLine($"{PadBy(3)}{{");
                stringBuilder.AppendLine($"{PadBy(4)}const string updateQuery = \"UPDATE {table.TableName} SET {sqlWhereClauses} WHERE {sqlWhereClausesForPrimaryKeys}\";");
                stringBuilder.AppendLine($"{PadBy(4)}var rowsAffected = await db.ExecuteAsync(updateQuery, {table.DataModelName.FirstCharToLower()});");
                stringBuilder.AppendLine($"{PadBy(4)}return rowsAffected;");
                stringBuilder.AppendLine($"{PadBy(3)}}}");
                stringBuilder.AppendLine($"{PadBy(2)}}}");
            }
            else
            {
                stringBuilder.AppendLine($"{PadBy(2)}// INFO: There are no primary keys for the Dapper code generation tool to generate update method(s).");
            }
        }

        private static string PadBy(int quantity, bool useSpaces = false, int spacesMultiplier = 4)
        {
            return (useSpaces ? " ".Repeat(spacesMultiplier) : "\t").Repeat(quantity);
        }
    }
}
