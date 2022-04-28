/* 
    ------------------- Code Monkey -------------------

    Thank you for downloading the Code Monkey Utilities
    I hope you find them useful in your projects
    If you have any questions use the contact form
    Cheers!

               unitycodemonkey.com
    --------------------------------------------------
 */

using System;
using UnityEngine;

namespace CodeMonkey.Utils {

    /*
     * Sprite in the World
     * */
    public class World_Sprite {
        
        private const int sortingOrderDefault = 5000;

        public GameObject gameObject;
        public Transform transform;
        private SpriteRenderer spriteRenderer;


        public static World_Sprite CreateDebugButton(Vector3 position, System.Action ClickFunc) {
            World_Sprite worldSprite = new World_Sprite(null, position, new Vector3(10, 10), Assets.i.s_White, Color.green, sortingOrderDefault);
            worldSprite.AddButton(ClickFunc, null, null);
            return worldSprite;
        }

        public static World_Sprite CreateDebugButton(Transform parent, Vector3 localPosition, System.Action ClickFunc) {
            World_Sprite worldSprite = new World_Sprite(parent, localPosition, new Vector3(10, 10), Assets.i.s_White, Color.green, sortingOrderDefault);
            worldSprite.AddButton(ClickFunc, null, null);
            return worldSprite;
        }

        public static World_Sprite CreateDebugButton(Transform parent, Vector3 localPosition, string text, System.Action ClickFunc, int fontSize = 30, float paddingX = 5, float paddingY = 5) {
            GameObject gameObject = new GameObject("DebugButton");
            gameObject.transform.parent = parent;
            gameObject.transform.localPosition = localPosition;
            TextMesh textMesh = UtilsClass.CreateWorldText(text, gameObject.transform, Vector3.zero, fontSize, Color.white, TextAnchor.MiddleCenter, TextAlignment.Center, 20000);
            Bounds rendererBounds = textMesh.GetComponent<MeshRenderer>().bounds;

            Color color = UtilsClass.GetColorFromString("00BA00FF");
            if (color.r >= 1f) color.r = .9f;
            if (color.g >= 1f) color.g = .9f;
            if (color.b >= 1f) color.b = .9f;
            Color colorOver = color * 1.1f; // button over color lighter

            World_Sprite worldSprite = new World_Sprite(gameObject.transform, Vector3.zero, rendererBounds.size + new Vector3(paddingX, paddingY), Assets.i.s_White, color, sortingOrderDefault);
            worldSprite.AddButton(ClickFunc, () => worldSprite.SetColor(colorOver), () => worldSprite.SetColor(color));
            return worldSprite;
        }

        public static World_Sprite Create(Transform parent, Vector3 localPosition, Vector3 localScale, Sprite sprite, Color color, int sortingOrderOffset) {
            return new World_Sprite(parent, localPosition, localScale, sprite, color, sortingOrderOffset);
        }

        public static World_Sprite Create(Transform parent, Vector3 localPosition, Vector3 localScale, Sprite sprite = null, Color? color = null, int sortingOrderOffset = 0) {
            if (sprite == null) {
                sprite = Assets.i.s_White;
            }
            if (color == null) {
                color = Color.white;
            }
            return new World_Sprite(parent, localPosition, localScale, sprite, (Color)color, sortingOrderOffset);
        }

        public static World_Sprite Create(Vector3 worldPosition, Sprite sprite) {
            return new World_Sprite(null, worldPosition, new Vector3(1, 1, 1), sprite, Color.white, 0);
        }

        public static World_Sprite Create(Vector3 worldPosition, Vector3 localScale, Sprite sprite, Color color, int sortingOrderOffset) {
            return new World_Sprite(null, worldPosition, localScale, sprite, color, sortingOrderOffset);
        }

        public static World_Sprite Create(Vector3 worldPosition, Vector3 localScale, Sprite sprite, Color color) {
            return new World_Sprite(null, worldPosition, localScale, sprite, color, 0);
        }

        public static World_Sprite Create(Vector3 worldPosition, Vector3 localScale, Color color) {
            return new World_Sprite(null, worldPosition, localScale, Assets.i.s_White, color, 0);
        }

        public static World_Sprite Create(Vector3 worldPosition, Vector3 localScale, Color color, int sortingOrderOffset) {
            return new World_Sprite(null, worldPosition, localScale, Assets.i.s_White, color, sortingOrderOffset);
        }

        public static World_Sprite Create(Vector3 worldPosition, Vector3 localScale) {
            return new World_Sprite(null, worldPosition, localScale, Assets.i.s_White, Color.white, 0);
        }

        public static World_Sprite Create(Vector3 worldPosition, Vector3 localScale, int sortingOrderOffset) {
            return new World_Sprite(null, worldPosition, localScale, Assets.i.s_White, Color.white, sortingOrderOffset);
        }

        public static int GetSortingOrder(Vector3 position, int offset, int baseSortingOrder = sortingOrderDefault) {
            return (int)(baseSortingOrder - position.y) + offset;
        }




        public World_Sprite(Transform parent, Vector3 localPosition, Vector3 localScale, Sprite sprite, Color color, int sortingOrderOffset) {
            Vector3 position = localPosition;
            if (parent != null) {
                position = parent.TransformPoint(position);
            }
            int sortingOrder = GetSortingOrder(position, sortingOrderOffset);
            gameObject = UtilsClass.CreateWorldSprite(parent, "Sprite", sprite, localPosition, localScale, sortingOrder, color);
            transform = gameObject.transform;
            spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        }

        public void SetActive(bool isActive) {
            gameObject.SetActive(isActive);
        }

        public SpriteRenderer GetSpriteRenderer() {
            return spriteRenderer;
        }

        public void SetName(string name) {
            gameObject.name = name;
        }

        public void SetSortingOrderOffset(int sortingOrderOffset) {
            SetSortingOrder(GetSortingOrder(gameObject.transform.position, sortingOrderOffset));
        }

        public void SetSortingOrder(int sortingOrder) {
            gameObject.GetComponent<SpriteRenderer>().sortingOrder = sortingOrder;
        }

        public void SetSortingLayerName(string layerName) {
            gameObject.GetComponent<SpriteRenderer>().sortingLayerName = layerName;
        }

        public int GetSortingOrder() {
            return gameObject.GetComponent<SpriteRenderer>().sortingOrder;
        }

        public void SetParent(GameObject parentGameObject) {
            SetParent(parentGameObject.transform);
        }

        public void SetParent(Transform parent) {
            transform.parent = parent;
        }

        public void SetLocalScale(Vector3 localScale) {
            transform.localScale = localScale;
        }

        public void SetPosition(Vector3 localPosition) {
            transform.localPosition = localPosition;
        }

        public void SetPosition(Func<Vector3> getLocalPosition) {
            FunctionUpdater.Create(() => { 
                transform.localPosition = getLocalPosition();
            });
        }

        public Vector3 GetPosition() {
            return transform.position;
        }

        public Vector3 GetLocalScale() {
            return transform.localScale;
        }

        public float GetRotation() {
            return transform.localEulerAngles.z;
        }

        public void SetRotation(float rotation) {
            transform.localEulerAngles = new Vector3(0, 0, rotation);
        }

        public void SetColor(Color color) {
            spriteRenderer.color = color;
        }

        public void SetSprite(Sprite sprite) {
            spriteRenderer.sprite = sprite;
        }

        public void SetFlip(bool flipX, bool flipY) {
            spriteRenderer.flipX = flipX;
            spriteRenderer.flipY = flipY;
        }

        public void Show() {
            gameObject.SetActive(true);
        }

        public void Hide() {
            gameObject.SetActive(false);
        }

        public Button_Sprite AddButton(System.Action ClickFunc, System.Action MouseOverOnceFunc, System.Action MouseOutOnceFunc) {
            gameObject.AddComponent<BoxCollider2D>();
            Button_Sprite buttonSprite = gameObject.AddComponent<Button_Sprite>();
            if (ClickFunc != null)
                buttonSprite.ClickFunc = ClickFunc;
            if (MouseOverOnceFunc != null)
                buttonSprite.MouseOverOnceFunc = MouseOverOnceFunc;
            if (MouseOutOnceFunc != null)
                buttonSprite.MouseOutOnceFunc = MouseOutOnceFunc;
            return buttonSprite;
        }

        public void DestroySelf() {
            UnityEngine.Object.Destroy(gameObject);
        }

    }

}