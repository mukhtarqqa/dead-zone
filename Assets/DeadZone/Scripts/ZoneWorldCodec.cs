using System;
using System.IO;
using System.IO.Compression;
using System.Text;
namespace DeadZone
{
    // World messages are bounded UTF-8 + Deflate. Inputs retain their small JSON wire format.
    public static class ZoneWorldCodec
    {
        public const int MaxWireBytes=192*1024,MaxDecodedBytes=1024*1024;
        public static int LastRawBytes{get;private set;}public static int LastWireBytes{get;private set;}
        public static byte[] Encode(string json)
        {
            var raw=Encoding.UTF8.GetBytes(json);if(raw.Length>MaxDecodedBytes)throw new InvalidDataException("World exceeds decoded byte budget: "+raw.Length);
            using var output=new MemoryStream();using(var stream=new DeflateStream(output,CompressionLevel.Fastest,true))stream.Write(raw,0,raw.Length);
            var encoded=output.ToArray();if(encoded.Length>MaxWireBytes)throw new InvalidDataException("World exceeds wire byte budget: "+encoded.Length);
            LastRawBytes=raw.Length;LastWireBytes=encoded.Length;return encoded;
        }
        public static string Decode(byte[] payload)
        {
            if(payload==null||payload.Length==0||payload.Length>MaxWireBytes)throw new InvalidDataException("Invalid world payload size");
            using var input=new MemoryStream(payload);using var stream=new DeflateStream(input,CompressionMode.Decompress);using var output=new MemoryStream();var buffer=new byte[8192];int read;
            while((read=stream.Read(buffer,0,buffer.Length))>0){if(output.Length+read>MaxDecodedBytes)throw new InvalidDataException("Inflated world exceeds byte budget");output.Write(buffer,0,read);}
            return Encoding.UTF8.GetString(output.ToArray());
        }
    }
}
