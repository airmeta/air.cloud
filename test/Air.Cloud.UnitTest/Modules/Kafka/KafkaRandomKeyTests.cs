using Air.Cloud.Modules.Kafka.Utils;

namespace Air.Cloud.UnitTest.Modules.Kafka
{
    public class KafkaRandomKeyTests
    {
        [Fact]
        public void GetRandom_should_return_value_inside_requested_range()
        {
            const int min = 10;
            const int max = 20;

            for (var i = 0; i < 100; i++)
            {
                var value = KafkaRandomKey.GetRandom(min, max);

                Assert.InRange(value, min, max - 1);
            }
        }

        [Fact]
        public void GetRandom_should_throw_when_range_is_invalid()
        {
            Assert.Throws<ArgumentException>(() => KafkaRandomKey.GetRandom(10, 10));
        }
    }
}
