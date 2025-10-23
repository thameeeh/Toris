using UnityEngine;

// PURPOSE: Interface attackers implement to describe their hit against the player.
// PlayerHurtbox queries for this and builds a HitData on contact.

public interface IHitPayloadProvider { HitData BuildHitData(Vector3 targetPosition); }
