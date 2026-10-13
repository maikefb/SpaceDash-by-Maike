using System;
using System.Diagnostics;
using LcdMod.Client.Apps;

static class Program
{
    static int Main()
    {
        var layout = new ConveyorLayout();

        var ladder = new[] { 3, 8, 0, 5, 1, 2, 6, 7, 4, 9, 2, 3, 7, 8, 1, 6, 0, 1, 5, 6, 3, 4, 8, 9, 2, 7 };
        layout.Reset(10);
        for (int i = 0; i < ladder.Length; i += 2)
            layout.AddEdge(ladder[i], ladder[i + 1]);
        layout.Run(3);
        CheckStructure(layout);
        Check(Crossings(layout) == 0, "ladder crossings = " + Crossings(layout));

        layout.Reset(7);
        for (int i = 1; i <= 6; i++)
            layout.AddEdge(0, i);
        layout.Run(2);
        CheckStructure(layout);
        Check(layout.Count > layout.RealCount, "star without dummies");
        for (int l = 0; l < layout.Columns; l++)
        {
            int real = 0;
            for (int v = 0; v < layout.RealCount; v++)
                if (layout.Layer[v] == l) real++;
            Check(real <= 2, "star layer " + l + " has " + real + " real nodes");
        }

        var random = new Random(42);
        layout.Reset(160);
        for (int i = 1; i < 160; i++)
            layout.AddEdge(random.Next(i), i);
        layout.Run(12);
        var watch = Stopwatch.StartNew();
        layout.Run(12);
        watch.Stop();
        CheckStructure(layout);
        Check(watch.Elapsed.TotalMilliseconds < 20, "tree run took " + watch.Elapsed.TotalMilliseconds + " ms");

        Console.WriteLine("tree: " + watch.Elapsed.TotalMilliseconds.ToString("0.000") + " ms, crossings " + Crossings(layout) +
                          ", " + layout.Columns + "x" + layout.Rows + ", dummies " + (layout.Count - layout.RealCount));
        Console.WriteLine("OK");
        return 0;
    }

    static void CheckStructure(ConveyorLayout layout)
    {
        for (int s = 0; s < layout.SegmentCount; s++)
        {
            int delta = Math.Abs(layout.Layer[layout.SegA[s]] - layout.Layer[layout.SegB[s]]);
            Check(delta == 1 || (delta == 0 && layout.SegSameLayer[s]), "segment " + s + " spans " + delta);
        }

        for (int a = 0; a < layout.Count; a++)
            for (int b = a + 1; b < layout.Count; b++)
                Check(layout.Layer[a] != layout.Layer[b] || layout.Row[a] != layout.Row[b], "nodes " + a + " and " + b + " overlap");
    }

    static int Crossings(ConveyorLayout layout)
    {
        int total = 0;
        for (int i = 0; i < layout.SegmentCount; i++)
            for (int j = i + 1; j < layout.SegmentCount; j++)
            {
                if (layout.SegSameLayer[i] || layout.SegSameLayer[j])
                    continue;
                int ai = layout.SegA[i], bi = layout.SegB[i], aj = layout.SegA[j], bj = layout.SegB[j];
                if (layout.Layer[ai] != layout.Layer[aj])
                    continue;
                if ((layout.Row[ai] - layout.Row[aj]) * (layout.Row[bi] - layout.Row[bj]) < 0)
                    total++;
            }
        return total;
    }

    static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
