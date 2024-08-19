using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text;

namespace Air.Cloud.Modules.Clairvoyance
{
    public class PiplinesHandler
    {
        private const int _minimumBufferSize = 512;
        private Socket _socket;
        private Pipe _pipe;

        public PiplinesHandler(Socket socket)
        {
            _socket = socket;
            var options = new PipeOptions(pauseWriterThreshold: 4096, resumeWriterThreshold: 1024);
            _pipe = new Pipe(options);
        }

        public async Task StartReceiveAsync()
        {
             Task recive= ReceiveMessageAsync();
            Task msg= ProcessMessageAsync();
            await Task.WhenAll(recive,msg);
        }


        private async Task ReceiveMessageAsync()
        {
            PipeWriter writer = _pipe.Writer;

            while (true)
            {
                try
                {
                    //从writer申请缓冲区
                    Memory<byte> memory = writer.GetMemory(_minimumBufferSize);
                    //从socket读取数据，直接写入到缓冲区中，即直接写入了PipeWriter中        
                    int bytesRead = await _socket.ReceiveAsync(memory, SocketFlags.None);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    //前移写标志位
                    writer.Advance(bytesRead);
                    //通知Reader，可以读取了
                    var result = await writer.FlushAsync();

                    if (result.IsCompleted)
                        break;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    break;
                }

            }

            await writer.CompleteAsync();

            try
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task ProcessMessageAsync()
        {
            PipeReader _pipeReader = _pipe.Reader;

            while (true)
            {
                //读取消息
                var result = await _pipeReader.ReadAsync();
                var buffer = result.Buffer;
                //查找结束符            
                SequencePosition? position = buffer.PositionOf((byte)'\n');

                if (position == null)
                {
                    continue;
                }

                // 处理消息
                var line = buffer.Slice(0, position.Value);
                string msg = Encoding.UTF8.GetString(line);
                Console.WriteLine(msg);

                // 前移PipeReader
                buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                _pipeReader.AdvanceTo(buffer.Start, buffer.End);

                // Stop reading if there's no more data coming.
                if (result.IsCompleted)
                {
                    break;
                }
            }

            await _pipeReader.CompleteAsync();
        }
    }
}