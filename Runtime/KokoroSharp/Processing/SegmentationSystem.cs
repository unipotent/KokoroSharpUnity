using KokoroSharp.Core;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System;
using static KokoroSharp.Processing.Tokenizer;

namespace KokoroSharp.Processing
{

    /// <summary> Helper class that allows turning text tokens into segments, allowing us to get the first response of the model quicker. </summary>
    /// <remarks> This allows us to begin playing back the audio of the first sentence, while the model processes the rest of the sequence on the background. </remarks>
    public static class SegmentationSystem
    {
        static int NLToken = Vocab['\n'];
        static HashSet<int> properEndSeqTokens = new HashSet<int> { Vocab['.'], Vocab['!'], Vocab['?'], Vocab[':'] };
        static HashSet<int> fallbackEndTokens = new HashSet<int> { Vocab[','], Vocab[' '] };

        static List<int> reusableTempList = new List<int>();

        /// <summary> Turns the input tokens into multiple segments, aggressively optimized for streaming. Then returns the segments in a list. </summary>
        /// <remarks> This is just so the audio can be played back with the first part, while the model is still processing the rest of the sequence. </remarks>
        /// <param name="segmentationStrategy">The segmentation strategy that will be used to chunk the tokens into segments.</param>
        public static List<int[]> SplitToSegments(int[] tokens, DefaultSegmentationConfig segmentationStrategy)
        {
            if (tokens.Length <= segmentationStrategy.MaxFirstSegmentLength)
            {
                return new List<int[]> { tokens };
            }

            List<int[]> segmentsList = new List<int[]>();
            // Go through the potential segments and finalize them.
            int totalTokensProcessed = 0;
            while (totalTokensProcessed < tokens.Length)
            {
                reusableTempList.Clear();
                var segmentRange = GetSegmentRange(segmentsList.Count);
                int min = segmentRange.Item1;
                int max = segmentRange.Item2;

                for (int i = 0; i < max && (totalTokensProcessed + i < tokens.Length); i++)
                {
                    reusableTempList.Add(tokens[totalTokensProcessed + i]);
                }

                // If there's a newline token, just end it! Do not look further! It's the perfect place to segment.
                if (reusableTempList.Contains(NLToken))
                {
                    AddRange(reusableTempList.IndexOf(NLToken) + 1);
                }
                if (reusableTempList.Count == 0) { continue; }

                foreach (var endSeqToken in properEndSeqTokens)
                { // Check if we can end the sequence properly here.
                    if (reusableTempList.Contains(endSeqToken))
                    { // They are ordered by highest preference. Periods are nice to end it.
                        AddRange((segmentsList.Count >= 2) ? reusableTempList.LastIndexOf(endSeqToken) : reusableTempList.IndexOf(endSeqToken));
                        break; // For the first two segments, we'll take the FIRST occasion for a quick response. For the rest, the last occasion.
                    }
                }
                if (reusableTempList.Count == 0) { continue; }

                // If there was no *proper* end_seq punctuation [.:!?] found on the phrase, we can start searching for fallback punctuation.
                foreach (var fallbackEndToken in fallbackEndTokens)
                {  // This includes comma and space at the moment, in this order.
                    if (reusableTempList.Contains(fallbackEndToken))
                    { // So, a split on a 'comma' character will be preferred over a split on 'space'.
                        AddRange(reusableTempList.LastIndexOf(fallbackEndToken));
                        break; // For the first segment, we'll take the FIRST occasion for a quick response. For the rest, the last occasion.
                    }
                }
                if (reusableTempList.Count == 0) { continue; }

                // If we met NEITHER a punctuation token NOR a space, let's try to check find the first index in which there's a punctuation around.
                reusableTempList.Clear();

                // Replace range operator with traditional array copying
                int remainingLength = tokens.Length - totalTokensProcessed;
                int[] remainingTokens = new int[remainingLength];
                Array.Copy(tokens, totalTokensProcessed, remainingTokens, 0, remainingLength);
                reusableTempList.AddRange(remainingTokens);

                foreach (var endSeqToken in properEndSeqTokens.Concat(fallbackEndTokens))
                {
                    if (reusableTempList.Contains(endSeqToken))
                    {
                        AddRange(reusableTempList.IndexOf(endSeqToken));
                        break;
                    }
                }
                if (reusableTempList.Count == 0) { continue; }

                // Well, at this point, there are NO punctuations available there. We either speak the whole thing, or we cut the stuff mid-word.
                // This is extremely edge-case, so any of the two will do. I don't expect any actual applications to stump into this.
                while (reusableTempList.Count > 0)
                {
                    var amountToAdd = Math.Min(reusableTempList.Count, 510);

                    // Replace collection expression with ToArray()
                    int[] segmentArray = new int[amountToAdd];
                    for (int i = 0; i < amountToAdd; i++)
                    {
                        segmentArray[i] = reusableTempList[i];
                    }
                    segmentsList.Add(segmentArray);

                    reusableTempList.RemoveRange(0, amountToAdd);
                }
                break;
            }

            return segmentsList;

            void AddRange(int count)
            {
                count = Math.Max(count, 1);
                int end() => totalTokensProcessed + count;
                var x = tokens[end()];
                while (end() < tokens.Length && tokens[end()] != NLToken && (properEndSeqTokens.Contains(tokens[end()]) || fallbackEndTokens.Contains(tokens[end()])))
                {
                    count++;
                }

                var newEnd = Math.Min(end(), tokens.Length - 1);
                while (newEnd > totalTokensProcessed && tokens[newEnd - 1] == Vocab[' '])
                {
                    newEnd--;
                }
                if (tokens[newEnd] != NLToken && Math.Abs(newEnd - tokens.Length) < 20)
                {
                    count += (tokens.Length - newEnd);
                    newEnd = tokens.Length;
                }
                if (newEnd > totalTokensProcessed + 1)
                {
                    // Replace range operator with traditional array copying
                    int segmentLength = newEnd - totalTokensProcessed;
                    int[] segment = new int[segmentLength];
                    Array.Copy(tokens, totalTokensProcessed, segment, 0, segmentLength);
                    segmentsList.Add(segment);
                }

                // Replace range operator in debug output
                int debugLength = newEnd - totalTokensProcessed;
                int[] debugTokens = new int[debugLength];
                Array.Copy(tokens, totalTokensProcessed, debugTokens, 0, debugLength);
                string debugText = new string(debugTokens.Select(x => TokenToChar[x]).ToArray());
                Debug.WriteLine($"[{segmentsList.Count}](+{count} [{totalTokensProcessed}/{tokens.Length}]): {debugText}".Replace("\n", "®"));

                totalTokensProcessed += count;
                reusableTempList.Clear();
            }

            (int, int, int) GetSegmentRange(int segmentIndex)
            {
                var ss = segmentationStrategy;
                if (segmentIndex == 0)
                {
                    return (Math.Min(ss.MinFirstSegmentLength, 3), ss.MaxFirstSegmentLength, (ss.MaxFirstSegmentLength - ss.MinFirstSegmentLength) / 2);
                }
                else if (segmentIndex == 1)
                {
                    return (0, ss.MaxSecondSegmentLength, ss.MaxSecondSegmentLength);
                }
                else
                {
                    return (ss.MinFollowupSegmentsLength, Math.Min(ss.MinFollowupSegmentsLength * 2, KokoroModel.maxTokens), ss.MinFollowupSegmentsLength);
                }
            }
        }
    }
}