namespace CG_Task1
{
    class Convolution
    {
        public double[,] Kernel { get; }

        public int Size { get; }

        public double KernelSum { get; }

        public Convolution(double[,] kernel)
        {
            Kernel = kernel;
            Size = kernel.GetLength(0);

            KernelSum = 0;
            for(int i = 0; i < Size; i++)
                for (int j = 0; j < Size; j++)
                    KernelSum += kernel[i, j];
        }
    }
}
