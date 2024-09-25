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
        public SqlTypeEnum SqlTypeEnum { get; }
        public SqlConstraintEnum SqlConstraintEnum { get; }


        private uint _maxVarDataLength = DEFAULT_MAX_VAR_DATA_LENGTH;


        public SqlTypeAttribute(Type sqlType, SqlTypeEnum sqlTypeEnum, SqlConstraintEnum sqlConstraintEnum = SqlConstraintEnum.NONE, uint maxVarLength = DEFAULT_MAX_VAR_DATA_LENGTH)
        {
            SqlType = sqlType;
            SqlTypeEnum = sqlTypeEnum;
            SqlConstraintEnum = sqlConstraintEnum;

            _maxVarDataLength = maxVarLength;
        }


        public string GetSqlTypeString()
        {
            string result = Enum.GetName(typeof(SqlTypeEnum), SqlTypeEnum)!;

            if (result.Contains("VAR"))
            {
                result += $"({_maxVarDataLength})";
            }
            //else if (SqlTypeEnum == SqlTypeEnum.UNIQUEIDENTIFIER)
            //{
            //    result +=
            //}

            return result;
        }


        public string GetSqlConstraintString()
        {
            string result = string.Empty;

            if (SqlConstraintEnum != SqlConstraintEnum.NONE)
            {
                result = Enum.GetName(typeof(SqlConstraintEnum), SqlConstraintEnum)!.Replace('_', ' ');
            }

            return result;
        }
    }
}
