using System.Buffers.Binary;
namespace DrovaVoices;
public static class WaveReader
{
    public static void ApplyGain(float[] samples,float decibels)
    {
        if(!float.IsFinite(decibels) || Math.Abs(decibels)>6)throw new InvalidDataException("Invalid playback gain");
        if(decibels==0)return;
        float factor=MathF.Pow(10,decibels/20);
        // Reject unsafe settings instead of clipping or compressing the performance.
        foreach(float sample in samples)
            if(!float.IsFinite(sample) || Math.Abs(sample*factor)>=1)throw new InvalidDataException("Playback gain would clip");
        for(int i=0;i<samples.Length;i++)samples[i]*=factor;
    }
    public static float[] Read(byte[] data)
    {
        if (data.Length<44 || System.Text.Encoding.ASCII.GetString(data,0,4)!="RIFF" || System.Text.Encoding.ASCII.GetString(data,8,4)!="WAVE") throw new InvalidDataException("Not a RIFF WAVE");
        long declared=BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4,4))+8L;
        if(declared>data.Length)throw new InvalidDataException("Truncated RIFF");
        bool format=false; int audioStart=-1,audioSize=0;
        for(int offset=12;offset+8<=declared;)
        {
            string chunk=System.Text.Encoding.ASCII.GetString(data,offset,4);
            uint size=BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset+4,4));long end=offset+8L+size;
            if(end>declared || size>int.MaxValue)throw new InvalidDataException("Truncated chunk");
            if(chunk=="fmt ")
            {
                if(size<16)throw new InvalidDataException("Invalid fmt");
                var value=data.AsSpan(offset+8,(int)size);
                format=BinaryPrimitives.ReadUInt16LittleEndian(value)==1 && BinaryPrimitives.ReadUInt16LittleEndian(value.Slice(2))==1 &&
                    BinaryPrimitives.ReadUInt32LittleEndian(value.Slice(4))==24000 && BinaryPrimitives.ReadUInt16LittleEndian(value.Slice(14))==16;
                if(!format)throw new InvalidDataException("Expected mono PCM16 24kHz");
            }
            else if(chunk=="data") {if(audioStart>=0)throw new InvalidDataException("Multiple data chunks");audioStart=offset+8;audioSize=(int)size;}
            offset=checked((int)(end+(size&1)));
        }
        if(!format || audioStart<0 || audioSize==0 || audioSize%2!=0)throw new InvalidDataException("Incomplete WAVE");
        float[] result=new float[audioSize/2];
        for(int i=0;i<result.Length;i++)result[i]=BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(audioStart+i*2,2))/32768f;
        return result;
    }
}
