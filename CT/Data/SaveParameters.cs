namespace CT.Data
{
    public class SaveParameters
    {
        private SaveParameters() { }

        public string ModelKeyPropertyName { get; set; }
        public string SqlColumnList { get; set; }
        public string SqlInsertColumnList { get; set; }
    }
}
