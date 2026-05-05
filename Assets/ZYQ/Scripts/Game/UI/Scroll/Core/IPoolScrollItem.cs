namespace ZYQ.Demo
{
    public interface IPoolScrollItem<TData>
    {
        void SetData(TData data, int index);
    }
}