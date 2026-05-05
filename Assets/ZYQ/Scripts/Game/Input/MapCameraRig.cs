using UnityEngine;

namespace ZYQ.Demo
{
    public class MapCameraRig
    {
        private readonly Camera camera;
        private readonly Transform mapRoot;
        private readonly HiddenObjectDemoConfig config;
        private readonly Vector3 initialMapPosition;
        private Bounds mapBounds;
        private Vector3 reboundVelocity;
        private float zoom;

        public float Zoom => zoom;

        public MapCameraRig(Camera camera, Transform mapRoot, HiddenObjectDemoConfig config, Bounds mapBounds)
        {
            this.camera = camera;
            this.mapRoot = mapRoot;
            this.config = config;
            initialMapPosition = mapRoot.position;
            this.mapBounds = mapBounds;
            FitCameraToInitialView();
            zoom = config.defaultZoom;
            ApplyZoom();
        }

        public void SetMapBounds(Bounds bounds)
        {
            mapBounds = bounds;
        }

        public void DragByScreenDelta(Vector2 screenDelta)
        {
            var worldDelta = ScreenToWorldDelta(screenDelta) * config.mapDragSpeed;
            mapRoot.position += worldDelta;
        }

        public void ZoomBy(float delta)
        {
            zoom = Mathf.Clamp(zoom + delta, config.minZoom, config.maxZoom);
            ApplyZoom();
        }

        public void Reset()
        {
            FitCameraToInitialView();
            zoom = config.defaultZoom;
            mapRoot.position = initialMapPosition;
            reboundVelocity = Vector3.zero;
            ApplyZoom();
        }

        public void FitCameraToInitialView()
        {
            if (camera == null) return;

            float defaultZoom = Mathf.Max(0.01f, config.defaultZoom);
            float visibleHeight = mapBounds.size.y * config.initialViewHeightRatio / defaultZoom;
            camera.orthographicSize = Mathf.Max(visibleHeight * 0.5f, 0.01f);
        }

        public void Tick(float dt)
        {
            var clamped = ClampMapPosition(mapRoot.position);
            mapRoot.position = Vector3.SmoothDamp(mapRoot.position, clamped, ref reboundVelocity, config.reboundTime, config.reboundDamping, dt);
        }

        public bool IsMapBeyondBounds(float epsilon = 0.01f)
        {
            return Vector3.Distance(mapRoot.position, ClampMapPosition(mapRoot.position)) > epsilon;
        }

        public Vector3 ScreenToWorldPoint(Vector2 screenPosition)
        {
            var world = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -camera.transform.position.z));
            world.z = 0f;
            return world;
        }

        public Vector3 ScreenToWorldDelta(Vector2 screenDelta)
        {
            float height = camera.orthographicSize * 2f;
            float unitsPerPixel = height / Mathf.Max(1, Screen.height);
            return new Vector3(screenDelta.x * unitsPerPixel, screenDelta.y * unitsPerPixel, 0f);
        }

        public Vector3 ClampSpotlightWorldPosition(Vector3 position, float radius)
        {
            float leak = radius * config.spotlightLeakRatio;
            var bounds = GetScaledMapBounds();
            position.x = Mathf.Clamp(position.x, bounds.min.x - leak, bounds.max.x + leak);
            position.y = Mathf.Clamp(position.y, bounds.min.y - leak, bounds.max.y + leak);
            position.z = 0f;
            return position;
        }

        public void FollowSpotlightNearScreenEdge(Vector3 spotlightWorld, float dt)
        {
            var screen = camera.WorldToScreenPoint(spotlightWorld);
            Vector2 delta = Vector2.zero;
            float padding = config.edgeFollowPadding;

            if (screen.x < padding) delta.x = padding - screen.x;
            else if (screen.x > Screen.width - padding) delta.x = Screen.width - padding - screen.x;

            if (screen.y < padding) delta.y = padding - screen.y;
            else if (screen.y > Screen.height - padding) delta.y = Screen.height - padding - screen.y;

            if (delta.sqrMagnitude <= 0.01f) return;

            mapRoot.position += ScreenToWorldDelta(delta) * config.edgeFollowSpeed * dt;
        }

        private void ApplyZoom()
        {
            mapRoot.localScale = Vector3.one * zoom;
        }

        private Vector3 ClampMapPosition(Vector3 position)
        {
            var bounds = GetScaledMapBounds(position);
            float halfHeight = camera.orthographicSize;
            float halfWidth = halfHeight * camera.aspect;

            if (bounds.size.x <= halfWidth * 2f) position.x = initialMapPosition.x;
            else
            {
                float minX = initialMapPosition.x + halfWidth - bounds.extents.x - mapBounds.center.x * zoom;
                float maxX = initialMapPosition.x + bounds.extents.x - halfWidth - mapBounds.center.x * zoom;
                position.x = Mathf.Clamp(position.x, minX, maxX);
            }

            if (bounds.size.y <= halfHeight * 2f) position.y = initialMapPosition.y;
            else
            {
                float minY = initialMapPosition.y + halfHeight - bounds.extents.y - mapBounds.center.y * zoom;
                float maxY = initialMapPosition.y + bounds.extents.y - halfHeight - mapBounds.center.y * zoom;
                position.y = Mathf.Clamp(position.y, minY, maxY);
            }

            position.z = 0f;
            return position;
        }

        private Bounds GetScaledMapBounds()
        {
            return GetScaledMapBounds(mapRoot.position);
        }

        private Bounds GetScaledMapBounds(Vector3 rootPosition)
        {
            var bounds = mapBounds;
            bounds.size *= zoom;
            bounds.center = rootPosition + mapBounds.center * zoom;
            return bounds;
        }
    }
}
