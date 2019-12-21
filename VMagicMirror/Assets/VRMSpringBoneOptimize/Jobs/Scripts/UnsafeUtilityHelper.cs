namespace VRM.Optimize
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static unsafe class UnsafeUtilityHelper
    {
        public static void* Malloc<T>(Allocator allocator, int length = 1) where T : struct
        {
            var size = UnsafeUtility.SizeOf<T>() * length;
            var ptr = UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<T>(), allocator);
            UnsafeUtility.MemClear(ptr, size);
            return ptr;
        }
    }
}
