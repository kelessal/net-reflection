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
                },
                Numbers=new[] {1,2,3}
            };
            obj.ConvertToExpando();
            var result= obj.ConvertToExpando();
        }
        [Fact]
        public void SetPathValueTest()
        {
            var item= new TestObject();
            item.SetPathValue("NTO.Index", -3);
            
        }
    }
}
