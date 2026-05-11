namespace HorrorGame.Core
{
    /// <summary>
    /// Optional callback contract for objects managed by ObjectPoolManager.
    /// </summary>
    public interface IPoolable
    {
        void OnRentFromPool();

        void OnReturnToPool();
    }
}
