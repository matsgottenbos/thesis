/*
 * Helper methods for arrays
*/

namespace DriverPlannerShared {
    public static class ArrayHelper {
        public static int[] InvertArray(int[] array) {
            int[] invertedArray = new int[array.Length];
            for (int i = 0; i < array.Length; i++) {
                invertedArray[i] = -array[i];
            }
            return invertedArray;
        }

        public static int[] AddArrays(int[] array1, int[] array2) {
            int[] addedArray = new int[array1.Length];
            for (int i = 0; i < array1.Length; i++) {
                addedArray[i] = array1[i] + array2[i];
            }
            return addedArray;
        }

        public static bool AreArraysEqual<T>(T[] array1, T[] array2) where T : IEquatable<T> {
            for (int i = 0; i < array1.Length; i++) {
                if (array1[i].Equals(array2[i])) return false;
            }
            return true;
        }
    }
}
