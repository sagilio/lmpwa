using LammpsWithAngle.Data;
using Xunit;

namespace LammpsWithAngle.UnitTests
{
    public class FormatTest
    {
        [Fact]
        public void ShouldFormatPosition()
        {
            // '输出的文件格式： 1（ID)  1(CHAINS)   1(TYPE) -0.100000(CHARGE)  16.139999390(X)    80.227996826(Y) 3.936000109(Z)
            var position = new Atom
            {
                Id = 1,
                Chain = 1,
                Type = (int) AtomType.O,
                Charge = -0.100000,
                X = 16.139999390,
                Y = 80.227996826,
                Z = 3.936000109
            };
            var formatString = position.ToString();
            Assert.Equal(position.Data, Atom.Parse(formatString).Data);
        }
    }
}
