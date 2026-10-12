using Cysharp.Threading.Tasks;

namespace GameJam.InGame.System
{
    public interface IGJContext
    {
        UniTask InitAsync();

        void WireContextAfterInit(GJContextMap contextMap);
    }
}
