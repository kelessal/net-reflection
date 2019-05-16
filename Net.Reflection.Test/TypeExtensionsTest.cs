using System;
using Xunit;

namespace Net.Reflection.Test
{
    public class TypeExtensionsTest
    {
        [Fact]
        public void ConvertToDictionaryTest()
        {
            var obj = new
            {
                Name = "hello",
                Address = new
                {
                    City = "İstanbul",
                    Countr = "Turkey"
                }
            };
            var result= obj.ConvertToDictionary();
        }
        [Fact]
        public void SetPathValueTest()
        {
            var item= new TestObject();
            item.NTO = new NestedTestObject();
            item.SetPathValue("NTO.PropA", 3);
            
        }
    }
}
