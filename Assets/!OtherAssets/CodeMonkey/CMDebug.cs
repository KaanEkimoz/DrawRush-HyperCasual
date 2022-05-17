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
using CodeMonkey.Utils;

namespace CodeMonkey {

    /*
     * Debug Class with various helper functions to quickly create buttons, text, etc
     * */
    public static class CMDebug {

        // Creates a Button in the World
        public static World_Sprite Button(Transform parent, Vector3 localPosition, string text, System.Action ClickFunc, int fontSize = 30, float paddingX = 5, float paddingY = 5) {
            return World_Sprite.CreateDebugButton(parent, localPosition, text, ClickFunc, fontSize, paddingX, paddingY);
        }

        // Creates a Button in the UI
        public static UI_Sprite ButtonUI(Vector2 anchoredPosition, string text, Action ClickFunc) {
            return UI_Sprite.CreateDebugButton(anchoredPosition, text, ClickFunc);
        }

        public static UI_Sprite ButtonUI(Transform parent, Vector2 anchoredPosition, string text, Action ClickFunc) {
            return UI_Sprite.CreateDebugButton(parent, anchoredPosition, text, ClickFunc);
        }

        // Creates a World Text object at the world position
        public static void Text(string text, Vector3 localPosition = default(Vector3), Transform parent = null, int fontSize = 40, Color? color = null, TextAnchor textAnchor = TextAnchor.UpperLeft, TextAlignment textAlignment = TextAlignment.Left, int sortingOrder = UtilsClass.sortingOrderDefault) {
            UtilsClass.CreateWorldText(text, parent, localPosition, fontSize, color, textAnchor, textAlignment, sortingOrder);
        }
        
        // World text pop up at mouse position
        public static void TextPopupMouse(string text, Vector3? offset = null) {
            if (offset == null) {
                offset = Vector3.one;
            }
            UtilsClass.CreateWorldTextPopup(text, UtilsClass.GetMouseWorldPosition() + (Vector3)offset);
        }

        // World text pop up at mouse position
        public static void TextPopupMouse(object obj, Vector3? offset = null) {
            TextPopupMouse(obj.ToString(), offset);
        }

        // Creates a Text pop up at the world position
        public static void TextPopup(string text, Vector3 position, float popupTime = 1f) {
            UtilsClass.CreateWorldTextPopup(text, position, popupTime);
        }

        // Text Updater in World, (parent == null) = world position
        public static FunctionUpdater TextUpdater(Func<string> GetTextFunc, Vector3 localPosition, Transform parent = null, int fontSize = 40, Color? color = null, TextAnchor textAnchor = TextAnchor.UpperLeft, TextAlignment textAlignment = TextAlignment.Left, int sortingOrder = UtilsClass.sortingOrderDefault) {
            return UtilsClass.CreateWorldTextUpdater(GetTextFunc, localPosition, parent, fontSize, color, textAnchor, textAlignment, sortingOrder);
        }

        // Text Updater in UI
        public static FunctionUpdater TextUpdaterUI(Func<string> GetTextFunc, Vector2 anchoredPosition) {
            return UtilsClass.CreateUITextUpdater(GetTextFunc, anchoredPosition);
        }

        // Text Updater always following mouse
        public static void MouseTextUpdater(Func<string> GetTextFunc, Vector3 positionOffset = default(Vector3)) {
            GameObject gameObject = new GameObject();
            FunctionUpdater.Create(() => {
                gameObject.transform.position = UtilsClass.GetMouseWorldPosition() + positionOffset;
                return false;
            });
            TextUpdater(GetTextFunc, Vector3.zero, gameObject.transform);
        }

        // Trigger Action on Key
        public static FunctionUpdater KeyCodeAction(KeyCode keyCode, Action onKeyDown) {
            return UtilsClass.CreateKeyCodeAction(keyCode, onKeyDown);
        }
        


        // Debug DrawLine to draw a projectile, turn Gizmos On
        public static void DebugProjectile(Vector3 from, Vector3 to, float speed, float projectileSize) {
            Vector3 dir = (to - from).normalized;
            Vector3 pos = from;
            FunctionUpdater.Create(() => {
                Debug.DrawLine(pos, pos + dir * projectileSize);
                float distanceBefore = Vector3.Distance(pos, to);
                pos += dir * speed * Time.deltaTime;
                float distanceAfter = Vector3.Distance(pos, to);
                if (distanceBefore < distanceAfter) {
                    return true;
                }
                return false;
            });
        }



        public static void SpritePopupMouse(Sprite sprite, float scale = 1f) {
            SpritePopup(UtilsClass.GetMouseWorldPosition(), sprite, scale);
        }


        public static void SpritePopup(Vector3 position, Sprite sprite, float scale) {
            float popupTime = 1f;
            GameObject gameObject = DrawSpriteTimedAlpha(position, sprite, scale, popupTime);

            Vector3 finalPopupPosition = position + new Vector3(0, 1, 0) * 20f;
            Transform transform = gameObject.transform;
            Vector3 moveAmount = (finalPopupPosition - position) / popupTime;

            FunctionUpdater.Create(delegate () {
                if (gameObject == null) {
                    return true;
                }
                transform.position += moveAmount * Time.unscaledDeltaTime;
                return false;
            }, "SpritePopup");
        }

        public static GameObject DrawSpriteTimed(Sprite sprite, float scale, float timer) {
            return DrawSpriteTimed(UtilsClass.GetMouseWorldPosition(), sprite, scale, timer);
        }

        public static GameObject DrawSpriteTimed(Vector3 position, Sprite sprite, float scale, float timer) {
            GameObject gameObject = new GameObject("SpriteTimed", typeof(SpriteRenderer));
            gameObject.transform.position = position;
            gameObject.transform.localScale = Vector3.one * scale;
            gameObject.GetComponent<SpriteRenderer>().sprite = sprite;

            GameObject.Destroy(gameObject, timer);

            return gameObject;
        }

        public static GameObject DrawSpriteTimedAlpha(Sprite sprite, float scale, float timer, float startDecayTimeNormalized = .8f) {
            return DrawSpriteTimedAlpha(UtilsClass.GetMouseWorldPosition(), sprite, scale, timer, startDecayTimeNormalized);
        }

        public static GameObject DrawSpriteTimedAlpha(Vector3 position, Sprite sprite, float scale, float timer, float startDecayTimeNormalized = .8f) {
            GameObject gameObject = DrawSpriteTimed(position, sprite, scale, timer);

            SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();

            float startAlphaDecayTime = timer * startDecayTimeNormalized;
            float totalAlphaDecayTime = timer - startAlphaDecayTime;
            float currentTime = 0f;

            FunctionUpdater.Create(() => {
                if (gameObject == null) {
                    return true;
                }
                currentTime += Time.unscaledDeltaTime;
                if (currentTime >= startAlphaDecayTime) {
                    spriteRenderer.color = new Color(1, 1, 1, Mathf.Lerp(1f, 0f, 1 - ((timer - currentTime) / totalAlphaDecayTime)));
                }
                return false;
            });

            return gameObject;
        }


    }

}