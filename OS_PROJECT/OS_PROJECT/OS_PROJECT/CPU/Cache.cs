using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OS_PROJECT
{
    enum CacheType
    {
        Instruction, Input, Output, None
    }

    class Cache
    {
        public CacheLocation[] Frames = new CacheLocation[4];
        public uint Indexer = 0;

        public Cache()
        {
            for (int i = 0; i < 4; i++)
            {
                Frames[i] = new CacheLocation();
            }
        }

        public bool HasPage(uint page)
        {
            int pageIndex = Array.FindIndex<CacheLocation>(Frames, f => f.Page == page);
            if (pageIndex == -1)
                return false;
            return true;
        }

        public bool HasFrame(uint frame)
        {
            int frameIndex = Array.FindIndex<CacheLocation>(Frames, f => f.Frame == frame);
            if (frameIndex == -1)
                return false;
            return true;
        }

        public void Write(uint data, uint frame, uint offset)
        {
            int frameIndex = Array.FindIndex<CacheLocation>(Frames, f => f.Frame == frame);
            if (frameIndex != -1)
            {
                Frames[frameIndex].Frame = frame;
                Frames[frameIndex].Page = MMU.CorrespondingPage(frame);
                Frames[frameIndex].FrameData[offset] = data;
            }
        }

        public void Write(uint[] data, uint frame)
        {
            int frameIndex = Array.FindIndex<CacheLocation>(Frames, f => f.Frame == frame);
            if (frameIndex != -1)
            {
                Frames[frameIndex].Frame = frame;
                Frames[frameIndex].Page = MMU.CorrespondingPage(frame);
                Frames[frameIndex].FrameData = data;
            }
        }

        public uint Read(uint frame, uint offset)
        {
            int frameIndex = Array.FindIndex<CacheLocation>(Frames, f => f.Frame == frame);
            if (frameIndex != -1)
            {
                return Frames[frameIndex].FrameData[offset];
            }
            else return 0;
        }

        public uint[] ReadFrame(uint frame)
        {
            int frameIndex = Array.FindIndex<CacheLocation>(Frames, f => f.Frame == frame);
            if (frameIndex != -1)
            {
                return Frames[frameIndex].FrameData;
            }
            return new uint[4];
        }
    }

    class CacheLocation
    {
        public uint[] FrameData = new uint[4];
        public uint Page;
        public uint Frame;
    }
}
