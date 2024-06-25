namespace Llama.Core.Utils
{
    public static class LlamaMath
    {
        public static int Min(int v1, int v2, params int[] values)
        {
            List<int> otherValues =
            [
                v2
            ];

            if (values != null)
            {
                otherValues.AddRange(values);
            }

            int returnValue = v1;

            foreach (int v in otherValues)
            {
                returnValue = Math.Min(returnValue, v);
            }

            return returnValue;
        }
    }
}