#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace VivaldiCustomLauncher {

    public class VersionNumberComparer: Comparer<string> {

        public override int Compare(string a, string b) {
            string[] aSegments = a.Split('.');
            string[] bSegments = b.Split('.');

            int segmentsToCompare = Math.Max(aSegments.Length, bSegments.Length);

            for (int segmentIndex = 0; segmentIndex < segmentsToCompare; segmentIndex++) {
                uint aSegment = uint.Parse(aSegments.ElementAtOrDefault(segmentIndex) ?? "0");
                uint bSegment = uint.Parse(bSegments.ElementAtOrDefault(segmentIndex) ?? "0");

                int segmentComparison = aSegment.CompareTo(bSegment);

                if (segmentComparison < 0) {
                    return -1;
                } else if (segmentComparison > 0) {
                    return 1;
                }
            }

            return 0;
        }

    }

}