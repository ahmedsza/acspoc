using Microsoft.CognitiveServices.Speech.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACSFunctions
{
    class BinaryAudioStreamWriter : PushAudioOutputStreamCallback
    {
        private readonly Stream _stream;

        public BinaryAudioStreamWriter(Stream stream)
        {
            _stream = stream;
        }

        public override uint Write(byte[] dataBuffer)
        {
            _stream.Write(dataBuffer, 0, dataBuffer.Length);
            return (uint)dataBuffer.Length;
        }

        public override void Close()
        {
            _stream.Close();
            base.Close();
        }
    }
}
