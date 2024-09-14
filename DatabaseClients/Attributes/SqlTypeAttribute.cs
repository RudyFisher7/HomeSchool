namespace DatabaseClients.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SqlTypeAttribute : Attribute
    {
        public string SqlTypeString { get; }
        public Type SqlType { get; }


        public SqlTypeAttribute(SqlTypeEnum sqlTypeString, Type sqlType)
        {
            SqlTypeString = Enum.GetName(typeof(SqlTypeEnum), sqlTypeString)!;
            SqlType = sqlType;
        }
    }
}
