using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace LammpsWithAngle.UnitTests
{
    public class SerializerTest
    {
        [Fact]
        public async Task ShouldSerialize()
        {
            string path = Path.Combine("TestFile", "source.lmp");
            var lammpsData = await LammpsDataSerializer.DeserializeFromFileAsync(path);
        }
    }
}