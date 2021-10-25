namespace dotDoc.LocalIpc.Tests
{
    public class TestClass
    {
        public string TextValue { get; set; }
        public int IntValue { get; set; }

        public override bool Equals(object obj) => obj is TestClass testClass && testClass.TextValue == this.TextValue && testClass.IntValue == this.IntValue;

        public override int GetHashCode() => base.GetHashCode();

        public override string ToString() => $"TextValue: {this.TextValue} IntValue: {this.IntValue}";
    }
}
