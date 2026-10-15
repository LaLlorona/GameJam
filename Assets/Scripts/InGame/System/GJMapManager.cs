using System;
using Cysharp.Threading.Tasks;
using LDtkUnity;
using UnityEngine;

namespace GameJam.InGame.System
{
    // Moves the LDtk collision layers onto the ground layer so ground checks and wall casts can target them,
    // and builds a box collider around the whole map that the camera is confined to.
    public class GJMapManager : IGJContext
    {
        private readonly LDtkComponentProject _ldtkMap;

        public BoxCollider2D MapBoundsCollider { get; private set; }

        public GJMapManager(GJController.GJControllerParameter parameter)
        {
            _ldtkMap = parameter.AssetHolder.LDtkMap;
        }

        public UniTask InitAsync()
        {
            if (_ldtkMap == null)
            {
                Debug.LogError("GJAssetHolder has no LDtk map assigned.");
                return UniTask.CompletedTask;
            }

            SetCollisionLayersToGround();
            CreateMapBoundsCollider();
            return UniTask.CompletedTask;
        }

        public void WireContextAfterInit(GJContextMap contextMap)
        {
        }

        private void SetCollisionLayersToGround()
        {
            var groundLayer = LayerMask.NameToLayer(GJDefine.GROUND_LAYER_NAME);
            if (groundLayer < 0)
            {
                Debug.LogError($"Layer '{GJDefine.GROUND_LAYER_NAME}' does not exist.");
                return;
            }

            foreach (var mapTransform in _ldtkMap.GetComponentsInChildren<Transform>(true))
            {
                if (mapTransform.name.IndexOf(GJDefine.MAP_COLLISION_OBJECT_NAME, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }
                SetLayerRecursively(mapTransform, groundLayer);
            }
        }

        private void CreateMapBoundsCollider()
        {
            var levels = _ldtkMap.GetComponentsInChildren<LDtkComponentLevel>(true);
            if (levels.Length == 0)
            {
                Debug.LogError($"LDtk map '{_ldtkMap.name}' has no levels. Map bounds collider was not created.");
                return;
            }

            // Union of every level so a multi-level world is covered as one area.
            var mapBounds = GetLevelBounds(levels[0]);
            for (var i = 1; i < levels.Length; i++)
            {
                mapBounds.Encapsulate(GetLevelBounds(levels[i]));
            }

            var boundsObject = new GameObject(GJDefine.MAP_BOUNDS_OBJECT_NAME);
            // Ignore Raycast keeps the trigger out of default physics queries.
            boundsObject.layer = LayerMask.NameToLayer(GJDefine.MAP_BOUNDS_LAYER_NAME);
            boundsObject.transform.position = new Vector3(mapBounds.center.x, mapBounds.center.y, 0f);
            MapBoundsCollider = boundsObject.AddComponent<BoxCollider2D>();
            // Trigger only: it marks the camera area and must not block anything.
            MapBoundsCollider.isTrigger = true;
            MapBoundsCollider.size = mapBounds.size;
        }

        private static Bounds GetLevelBounds(LDtkComponentLevel level)
        {
            // The imported bounds are stale if the map was moved in the scene.
            level.RecalculateRects();
            return level.BorderBounds;
        }

        // The tilemap and its collider are children of the LDtk layer object, so the whole subtree is moved.
        private static void SetLayerRecursively(Transform target, int layer)
        {
            target.gameObject.layer = layer;
            foreach (Transform child in target)
            {
                SetLayerRecursively(child, layer);
            }
        }
    }
}
