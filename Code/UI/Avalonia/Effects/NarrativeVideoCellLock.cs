using System;
using System.Runtime.InteropServices;

namespace RPGGame.UI.Avalonia.Effects
{
    /// <summary>
    /// Maps a decoded video frame onto the character-cell grid so horizontal features
    /// cannot beat against line spacing (moiré / drift down the combat log).
    /// Each cell is filled from the center sample of its proportional source rectangle.
    /// </summary>
    public static class NarrativeVideoCellLock
    {
        /// <summary>
        /// Writes a cell-locked BGRA preview of <paramref name="sourceBgra"/> into
        /// <paramref name="destBgra"/>. Destination size must be
        /// <c>gridCols×cellPixelWidth</c> by <c>gridRows×cellPixelHeight</c>.
        /// </summary>
        public static void BlitCenterSampledCells(
            ReadOnlySpan<byte> sourceBgra,
            int sourceWidth,
            int sourceHeight,
            int sourceStrideBytes,
            Span<byte> destBgra,
            int gridCols,
            int gridRows,
            int cellPixelWidth,
            int cellPixelHeight)
        {
            if (!TryGetDestSize(sourceWidth, sourceHeight, sourceStrideBytes, gridCols, gridRows, cellPixelWidth, cellPixelHeight,
                    out int destWidth, out int destBytes))
                return;
            if (sourceBgra.Length < sourceStrideBytes * sourceHeight)
                throw new ArgumentException("sourceBgra is too small for sourceStrideBytes×sourceHeight.", nameof(sourceBgra));
            if (destBgra.Length < destBytes)
                throw new ArgumentException("destBgra is too small for the cell grid.", nameof(destBgra));

            for (int gy = 0; gy < gridRows; gy++)
            {
                int srcY = SampleCoord(gy, gridRows, sourceHeight);
                for (int gx = 0; gx < gridCols; gx++)
                {
                    int srcX = SampleCoord(gx, gridCols, sourceWidth);
                    int srcIndex = srcY * sourceStrideBytes + srcX * 4;
                    FillCell(
                        destBgra, destWidth, gx, gy, cellPixelWidth, cellPixelHeight,
                        sourceBgra[srcIndex], sourceBgra[srcIndex + 1], sourceBgra[srcIndex + 2], sourceBgra[srcIndex + 3]);
                }
            }
        }

        /// <summary>
        /// Same as the span overload, sampling RV32 pixels from an unmanaged decode buffer.
        /// </summary>
        public static void BlitCenterSampledCells(
            IntPtr sourceBgra,
            int sourceWidth,
            int sourceHeight,
            int sourceStrideBytes,
            Span<byte> destBgra,
            int gridCols,
            int gridRows,
            int cellPixelWidth,
            int cellPixelHeight)
        {
            if (sourceBgra == IntPtr.Zero)
                return;
            if (!TryGetDestSize(sourceWidth, sourceHeight, sourceStrideBytes, gridCols, gridRows, cellPixelWidth, cellPixelHeight,
                    out int destWidth, out int destBytes))
                return;
            if (destBgra.Length < destBytes)
                throw new ArgumentException("destBgra is too small for the cell grid.", nameof(destBgra));

            for (int gy = 0; gy < gridRows; gy++)
            {
                int srcY = SampleCoord(gy, gridRows, sourceHeight);
                for (int gx = 0; gx < gridCols; gx++)
                {
                    int srcX = SampleCoord(gx, gridCols, sourceWidth);
                    IntPtr p = IntPtr.Add(sourceBgra, srcY * sourceStrideBytes + srcX * 4);
                    byte b = Marshal.ReadByte(p, 0);
                    byte g = Marshal.ReadByte(p, 1);
                    byte r = Marshal.ReadByte(p, 2);
                    byte a = Marshal.ReadByte(p, 3);
                    FillCell(destBgra, destWidth, gx, gy, cellPixelWidth, cellPixelHeight, b, g, r, a);
                }
            }
        }

        /// <summary>
        /// Maps grid cell index → source pixel at the center of that cell's proportional band.
        /// </summary>
        public static int SampleCoord(int cellIndex, int cellCount, int sourceSize)
        {
            if (cellCount <= 0 || sourceSize <= 0)
                return 0;
            if (cellIndex < 0)
                cellIndex = 0;
            if (cellIndex >= cellCount)
                cellIndex = cellCount - 1;
            // (i + 0.5) / N * size, floored and clamped.
            int coord = (int)(((cellIndex + 0.5) * sourceSize) / cellCount);
            if (coord < 0)
                return 0;
            if (coord >= sourceSize)
                return sourceSize - 1;
            return coord;
        }

        private static bool TryGetDestSize(
            int sourceWidth,
            int sourceHeight,
            int sourceStrideBytes,
            int gridCols,
            int gridRows,
            int cellPixelWidth,
            int cellPixelHeight,
            out int destWidth,
            out int destBytes)
        {
            destWidth = 0;
            destBytes = 0;
            if (sourceWidth <= 0 || sourceHeight <= 0 || sourceStrideBytes < sourceWidth * 4)
                return false;
            if (gridCols <= 0 || gridRows <= 0 || cellPixelWidth <= 0 || cellPixelHeight <= 0)
                return false;
            destWidth = gridCols * cellPixelWidth;
            int destHeight = gridRows * cellPixelHeight;
            destBytes = destWidth * destHeight * 4;
            return true;
        }

        private static void FillCell(
            Span<byte> destBgra,
            int destWidth,
            int gx,
            int gy,
            int cellPixelWidth,
            int cellPixelHeight,
            byte b,
            byte g,
            byte r,
            byte a)
        {
            int x0 = gx * cellPixelWidth;
            int y0 = gy * cellPixelHeight;
            for (int y = 0; y < cellPixelHeight; y++)
            {
                int row = (y0 + y) * destWidth * 4;
                for (int x = 0; x < cellPixelWidth; x++)
                {
                    int i = row + (x0 + x) * 4;
                    destBgra[i] = b;
                    destBgra[i + 1] = g;
                    destBgra[i + 2] = r;
                    destBgra[i + 3] = a;
                }
            }
        }
    }
}
