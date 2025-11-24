using UnityEngine;

public class EffectsBootstrap : MonoBehaviour
{
    [SerializeField] private Transform effectsRoot;

    void Start()
    {
        if (effectsRoot == null)
            effectsRoot = transform;

        //Debug.Log("Effect runtime created!", this);

        var runtime = new EffectRuntimePool(effectsRoot);
        EffectManagerBehavior.BehaviorInstance.ConfigureRuntime(runtime);
    }
}

