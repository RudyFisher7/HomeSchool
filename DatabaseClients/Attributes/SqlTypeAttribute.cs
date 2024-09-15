namespace DataRepositories.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SqlTypeAttribute : Attribute
    {
        /// <summary>
        /// The default (MAX) parameter for variable-length SQL data types.
        /// </summary>
        /// <remarks>
        /// Check the data type's documentation to see the valid min and max
        /// lengths for each for the system being used; this class doesn't
        /// currently constrain these ranges since they can differ.
        /// </remarks>
        public const uint DEFAULT_MAX_VAR_DATA_LENGTH = 255u;

        public Type SqlType { get; }
        public string SqlTypeString { get; } = string.Empty;
        public string SqlConstraintString { get; } = string.Empty;


        public SqlTypeAttribute(Type sqlType, SqlTypeEnum sqlTypeEnum, SqlConstraintEnum sqlConstraintEnum = SqlConstraintEnum.NONE, uint maxVarLength = DEFAULT_MAX_VAR_DATA_LENGTH)
        {
            SqlType = sqlType;
            SqlTypeString = Enum.GetName(typeof(SqlTypeEnum), sqlTypeEnum)!;

            if (SqlTypeString.Contains("VAR"))
            {
                SqlTypeString += $"({maxVarLength})";
            }

            if (sqlConstraintEnum != SqlConstraintEnum.NONE)
            {
                SqlConstraintString = Enum.GetName(typeof(SqlConstraintEnum), sqlConstraintEnum)!.Replace('_', ' ');
            }
        }
    }
}
