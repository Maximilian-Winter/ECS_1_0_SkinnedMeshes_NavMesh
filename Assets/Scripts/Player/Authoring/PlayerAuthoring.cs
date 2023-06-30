using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct PlayerComponent : IComponentData
{
    public float MovementSpeed;
    public float RotationSpeed;
    public float CurrentSpeed;
}
public class PlayerAuthoring : MonoBehaviour
{
    public float MovementSpeed;
    public float RotationSpeed;
    
}

public class PlayerAuthoringBaker : Baker <PlayerAuthoring>
{
    public override void Bake( PlayerAuthoring authoring )
    {
        AddComponent<PlayerComponent>(new PlayerComponent
        {
            MovementSpeed = authoring.MovementSpeed,
            RotationSpeed = authoring.RotationSpeed,
            CurrentSpeed = 0.0f,
        });
    }
}
