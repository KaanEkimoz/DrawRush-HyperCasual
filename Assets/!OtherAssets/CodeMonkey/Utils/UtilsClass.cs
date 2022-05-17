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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CodeMonkey.Utils {

    /*
     * Various assorted utilities functions
     * */
    public static class UtilsClass {
        
        private static readonly Vector3 Vector3zero = Vector3.zero;
        private static readonly Vector3 Vector3one = Vector3.one;
        private static readonly Vector3 Vector3yDown = new Vector3(0,-1);

        public const int sortingOrderDefault = 5000;
        
        // Get Sorting order to set SpriteRenderer sortingOrder, higher position = lower sortingOrder
        public static int GetSortingOrder(Vector3 position, int offset, int baseSortingOrder = sortingOrderDefault) {
            return (int)(baseSortingOrder - position.y) + offset;
        }


        // Get Main Canvas Transform
        private static Transform cachedCanvasTransform;
        public static Transform GetCanvasTransform() {
            if (cachedCanvasTransform == null) {
                Canvas canvas = MonoBehaviour.FindObjectOfType<Canvas>();
                if (canvas != null) {
                    cachedCanvasTransform = canvas.transform;
                }
            }
            return cachedCanvasTransform;
        }

        // Get Default Unity Font, used in text objects if no font given
        public static Font GetDefaultFont() {
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }


        // Create a Sprite in the World, no parent
        public static GameObject CreateWorldSprite(string name, Sprite sprite, Vector3 position, Vector3 localScale, int sortingOrder, Color color) {
            return CreateWorldSprite(null, name, sprite, position, localScale, sortingOrder, color);
        }
        
        // Create a Sprite in the World
        public static GameObject CreateWorldSprite(Transform parent, string name, Sprite sprite, Vector3 localPosition, Vector3 localScale, int sortingOrder, Color color) {
            GameObject gameObject = new GameObject(name, typeof(SpriteRenderer));
            Transform transform = gameObject.transform;
            transform.SetParent(parent, false);
            transform.localPosition = localPosition;
            transform.localScale = localScale;
            SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = sortingOrder;
            spriteRenderer.color = color;
            return gameObject;
        }

        // Create a Sprite in the World with Button_Sprite, no parent
        public static Button_Sprite CreateWorldSpriteButton(string name, Sprite sprite, Vector3 localPosition, Vector3 localScale, int sortingOrder, Color color) {
            return CreateWorldSpriteButton(null, name, sprite, localPosition, localScale, sortingOrder, color);
        }

        // Create a Sprite in the World with Button_Sprite
        public static Button_Sprite CreateWorldSpriteButton(Transform parent, string name, Sprite sprite, Vector3 localPosition, Vector3 localScale, int sortingOrder, Color color) {
            GameObject gameObject = CreateWorldSprite(parent, name, sprite, localPosition, localScale, sortingOrder, color);
            gameObject.AddComponent<BoxCollider2D>();
            Button_Sprite buttonSprite = gameObject.AddComponent<Button_Sprite>();
            return buttonSprite;
        }

        // Creates a Text Mesh in the World and constantly updates it
        public static FunctionUpdater CreateWorldTextUpdater(Func<string> GetTextFunc, Vector3 localPosition, Transform parent = null, int fontSize = 40, Color? color = null, TextAnchor textAnchor = TextAnchor.UpperLeft, TextAlignment textAlignment = TextAlignment.Left, int sortingOrder = sortingOrderDefault) {
            TextMesh textMesh = CreateWorldText(GetTextFunc(), parent, localPosition, fontSize, color, textAnchor, textAlignment, sortingOrder);
            return FunctionUpdater.Create(() => {
                textMesh.text = GetTextFunc();
                return false;
            }, "WorldTextUpdater");
        }

        // Create Text in the World
        public static TextMesh CreateWorldText(string text, Transform parent = null, Vector3 localPosition = default(Vector3), int fontSize = 40, Color? color = null, TextAnchor textAnchor = TextAnchor.UpperLeft, TextAlignment textAlignment = TextAlignment.Left, int sortingOrder = sortingOrderDefault) {
            if (color == null) color = Color.white;
            return CreateWorldText(parent, text, localPosition, fontSize, (Color)color, textAnchor, textAlignment, sortingOrder);
        }
        
        // Create Text in the World
        public static TextMesh CreateWorldText(Transform parent, string text, Vector3 localPosition, int fontSize, Color color, TextAnchor textAnchor, TextAlignment textAlignment, int sortingOrder) {
            GameObject gameObject = new GameObject("World_Text", typeof(TextMesh));
            Transform transform = gameObject.transform;
            transform.SetParent(parent, false);
            transform.localPosition = localPosition;
            transform.localScale /= fontSize / 5;
            
            //for 3D Games
            transform.eulerAngles = new Vector3(90f, transform.eulerAngles.y, transform.eulerAngles.z);
            
            TextMesh textMesh = gameObject.GetComponent<TextMesh>();
            textMesh.anchor = textAnchor;
            textMesh.alignment = textAlignment;
            textMesh.text = text;
            textMesh.fontSize = fontSize;
            textMesh.color = color;
            textMesh.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;
            return textMesh;
        }


        // Create a Text Popup in the World, no parent
        public static void CreateWorldTextPopup(string text, Vector3 localPosition, float popupTime = 1f) {
            CreateWorldTextPopup(null, text, localPosition, 40, Color.white, localPosition + new Vector3(0, 20), popupTime);
        }
        
        // Create a Text Popup in the World
        public static void CreateWorldTextPopup(Transform parent, string text, Vector3 localPosition, int fontSize, Color color, Vector3 finalPopupPosition, float popupTime) {
            TextMesh textMesh = CreateWorldText(parent, text, localPosition, fontSize, color, TextAnchor.LowerLeft, TextAlignment.Left, sortingOrderDefault);
            Transform transform = textMesh.transform;
            Vector3 moveAmount = (finalPopupPosition - localPosition) / popupTime;
            FunctionUpdater.Create(delegate () {
                transform.position += moveAmount * Time.unscaledDeltaTime;
                popupTime -= Time.unscaledDeltaTime;
                if (popupTime <= 0f) {
                    UnityEngine.Object.Destroy(transform.gameObject);
                    return true;
                } else {
                    return false;
                }
            }, "WorldTextPopup");
        }

        // Create Text Updater in UI
        public static FunctionUpdater CreateUITextUpdater(Func<string> GetTextFunc, Vector2 anchoredPosition) {
            Text text = DrawTextUI(GetTextFunc(), anchoredPosition,  20, GetDefaultFont());
            return FunctionUpdater.Create(() => {
                text.text = GetTextFunc();
                return false;
            }, "UITextUpdater");
        }


        // Draw a UI Sprite
        public static RectTransform DrawSprite(Color color, Transform parent, Vector2 pos, Vector2 size, string name = null) {
            RectTransform rectTransform = DrawSprite(null, color, parent, pos, size, name);
            return rectTransform;
        }
        
        // Draw a UI Sprite
        public static RectTransform DrawSprite(Sprite sprite, Transform parent, Vector2 pos, Vector2 size, string name = null) {
            RectTransform rectTransform = DrawSprite(sprite, Color.white, parent, pos, size, name);
            return rectTransform;
        }
        
        // Draw a UI Sprite
        public static RectTransform DrawSprite(Sprite sprite, Color color, Transform parent, Vector2 pos, Vector2 size, string name = null) {
            // Setup icon
            if (name == null || name == "") name = "Sprite";
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform goRectTransform = go.GetComponent<RectTransform>();
            goRectTransform.SetParent(parent, false);
            goRectTransform.sizeDelta = size;
            goRectTransform.anchoredPosition = pos;

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;

            return goRectTransform;
        }

        public static Text DrawTextUI(string textString, Vector2 anchoredPosition, int fontSize, Font font) {
            return DrawTextUI(textString, GetCanvasTransform(), anchoredPosition, fontSize, font);
        }

        public static Text DrawTextUI(string textString, Transform parent, Vector2 anchoredPosition, int fontSize, Font font) {
            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(parent, false);
            Transform textGoTrans = textGo.transform;
            textGoTrans.SetParent(parent, false);
            textGoTrans.localPosition = Vector3zero;
            textGoTrans.localScale = Vector3one;

            RectTransform textGoRectTransform = textGo.GetComponent<RectTransform>();
            textGoRectTransform.sizeDelta = new Vector2(0,0);
            textGoRectTransform.anchoredPosition = anchoredPosition;

            Text text = textGo.GetComponent<Text>();
            text.text = textString;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.alignment = TextAnchor.MiddleLeft;
            if (font == null) font = GetDefaultFont();
            text.font = font;
            text.fontSize = fontSize;

            return text;
        }


        // Parse a float, return default if failed
	    public static float Parse_Float(string txt, float _default) {
		    float f;
		    if (!float.TryParse(txt, out f)) {
			    f = _default;
		    }
		    return f;
	    }
        
        // Parse a int, return default if failed
	    public static int Parse_Int(string txt, int _default) {
		    int i;
		    if (!int.TryParse(txt, out i)) {
			    i = _default;
		    }
		    return i;
	    }

	    public static int Parse_Int(string txt) {
            return Parse_Int(txt, -1);
	    }



        // Get Mouse Position in World with Z = 0f
        public static Vector3 GetMouseWorldPosition() {
            Vector3 vec = GetMouseWorldPositionWithZ(Input.mousePosition, Camera.main);
            vec.z = 0f;
            return vec;
        }

        public static Vector3 GetMouseWorldPositionWithZ() {
            return GetMouseWorldPositionWithZ(Input.mousePosition, Camera.main);
        }

        public static Vector3 GetMouseWorldPositionWithZ(Camera worldCamera) {
            return GetMouseWorldPositionWithZ(Input.mousePosition, worldCamera);
        }

        public static Vector3 GetMouseWorldPositionWithZ(Vector3 screenPosition, Camera worldCamera) {
            Vector3 worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
            return worldPosition;
        }

        public static Vector3 GetDirToMouse(Vector3 fromPosition) {
            Vector3 mouseWorldPosition = GetMouseWorldPosition();
            return (mouseWorldPosition - fromPosition).normalized;
        }

        

        // Is Mouse over a UI Element? Used for ignoring World clicks through UI
        public static bool IsPointerOverUI() {
            if (EventSystem.current.IsPointerOverGameObject()) {
                return true;
            } else {
                PointerEventData pe = new PointerEventData(EventSystem.current);
                pe.position =  Input.mousePosition;
                List<RaycastResult> hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll( pe, hits );
                return hits.Count > 0;
            }
        }


        
		// Returns 00-FF, value 0->255
	    public static string Dec_to_Hex(int value) {
		    return value.ToString("X2");
	    }

		// Returns 0-255
	    public static int Hex_to_Dec(string hex) {
		    return Convert.ToInt32(hex, 16);
	    }
        
		// Returns a hex string based on a number between 0->1
	    public static string Dec01_to_Hex(float value) {
		    return Dec_to_Hex((int)Mathf.Round(value*255f));
	    }

		// Returns a float between 0->1
	    public static float Hex_to_Dec01(string hex) {
		    return Hex_to_Dec(hex)/255f;
	    }

        // Get Hex Color FF00FF
	    public static string GetStringFromColor(Color color) {
		    string red = Dec01_to_Hex(color.r);
		    string green = Dec01_to_Hex(color.g);
		    string blue = Dec01_to_Hex(color.b);
		    return red+green+blue;
	    }
        
        // Get Hex Color FF00FFAA
	    public static string GetStringFromColorWithAlpha(Color color) {
		    string alpha = Dec01_to_Hex(color.a);
		    return GetStringFromColor(color)+alpha;
	    }

        // Sets out values to Hex String 'FF'
	    public static void GetStringFromColor(Color color, out string red, out string green, out string blue, out string alpha) {
		    red = Dec01_to_Hex(color.r);
		    green = Dec01_to_Hex(color.g);
		    blue = Dec01_to_Hex(color.b);
		    alpha = Dec01_to_Hex(color.a);
	    }
        
        // Get Hex Color FF00FF
	    public static string GetStringFromColor(float r, float g, float b) {
		    string red = Dec01_to_Hex(r);
		    string green = Dec01_to_Hex(g);
		    string blue = Dec01_to_Hex(b);
		    return red+green+blue;
	    }
        
        // Get Hex Color FF00FFAA
	    public static string GetStringFromColor(float r, float g, float b, float a) {
		    string alpha = Dec01_to_Hex(a);
		    return GetStringFromColor(r,g,b)+alpha;
	    }
        
        // Get Color from Hex string FF00FFAA
	    public static Color GetColorFromString(string color) {
		    float red = Hex_to_Dec01(color.Substring(0,2));
		    float green = Hex_to_Dec01(color.Substring(2,2));
		    float blue = Hex_to_Dec01(color.Substring(4,2));
            float alpha = 1f;
            if (color.Length >= 8) {
                // Color string contains alpha
                alpha = Hex_to_Dec01(color.Substring(6,2));
            }
		    return new Color(red, green, blue, alpha);
	    }

        // Return a color going from Red to Yellow to Green, like a heat map
        public static Color GetRedGreenColor(float value) {
            float r = 0f;
            float g = 0f;
            if (value <= .5f) {
                r = 1f;
                g = value * 2f;
            } else {
                g = 1f;
                r = 1f - (value - .5f) * 2f;
            }
            return new Color(r, g, 0f, 1f);
        }


        public static Color GetRandomColor() {
            return new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), 1f);
        }

        private static int sequencialColorIndex = -1;
        private static Color[] sequencialColors = new[] {
            GetColorFromString("26a6d5"),
            GetColorFromString("41d344"),
            GetColorFromString("e6e843"),
            GetColorFromString("e89543"),
            GetColorFromString("0f6ad0"),//("d34141"),
		    GetColorFromString("b35db6"),
            GetColorFromString("c45947"),
            GetColorFromString("9447c4"),
            GetColorFromString("4756c4"),
        };

        public static void ResetSequencialColors() {
            sequencialColorIndex = -1;
        }

        public static Color GetSequencialColor() {
            sequencialColorIndex = (sequencialColorIndex + 1) % sequencialColors.Length;
            return sequencialColors[sequencialColorIndex];
        }

        public static Color GetColor255(float red, float green, float blue, float alpha = 255f) {
            return new Color(red / 255f, green / 255f, blue / 255f, alpha / 255f);
        }


        // Generate random normalized direction
        public static Vector3 GetRandomDir() {
            return new Vector3(UnityEngine.Random.Range(-1f,1f), UnityEngine.Random.Range(-1f,1f)).normalized;
        }

        // Generate random normalized direction
        public static Vector3 GetRandomDirXZ() {
            return new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)).normalized;
        }


        public static Vector3 GetVectorFromAngle(int angle) {
            // angle = 0 -> 360
            float angleRad = angle * (Mathf.PI/180f);
            return new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
        }
        
        public static Vector3 GetVectorFromAngle(float angle) {
            // angle = 0 -> 360
            float angleRad = angle * (Mathf.PI/180f);
            return new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
        }
        
        public static Vector3 GetVectorFromAngleInt(int angle) {
            // angle = 0 -> 360
            float angleRad = angle * (Mathf.PI/180f);
            return new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
        }

        public static float GetAngleFromVectorFloat(Vector3 dir) {
            dir = dir.normalized;
            float n = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (n < 0) n += 360;

            return n;
        }

        public static float GetAngleFromVectorFloatXZ(Vector3 dir) {
            dir = dir.normalized;
            float n = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
            if (n < 0) n += 360;

            return n;
        }

        public static int GetAngleFromVector(Vector3 dir) {
            dir = dir.normalized;
            float n = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (n < 0) n += 360;
            int angle = Mathf.RoundToInt(n);

            return angle;
        }

        public static int GetAngleFromVector180(Vector3 dir) {
            dir = dir.normalized;
            float n = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            int angle = Mathf.RoundToInt(n);

            return angle;
        }

        public static Vector3 ApplyRotationToVector(Vector3 vec, Vector3 vecRotation) {
            return ApplyRotationToVector(vec, GetAngleFromVectorFloat(vecRotation));
        }

        public static Vector3 ApplyRotationToVector(Vector3 vec, float angle) {
            return Quaternion.Euler(0, 0, angle) * vec;
        }

        public static Vector3 ApplyRotationToVectorXZ(Vector3 vec, float angle) {
            return Quaternion.Euler(0, angle, 0) * vec;
        }



        public static FunctionUpdater CreateMouseDraggingAction(Action<Vector3> onMouseDragging) {
            return CreateMouseDraggingAction(0, onMouseDragging);
        }

        public static FunctionUpdater CreateMouseDraggingAction(int mouseButton, Action<Vector3> onMouseDragging) {
            bool dragging = false;
            return FunctionUpdater.Create(() => {
                if (Input.GetMouseButtonDown(mouseButton)) {
                    dragging = true;
                }
                if (Input.GetMouseButtonUp(mouseButton)) {
                    dragging = false;
                }
                if (dragging) {
                    onMouseDragging(UtilsClass.GetMouseWorldPosition());
                }
                return false; 
            });
        }

        public static FunctionUpdater CreateMouseClickFromToAction(Action<Vector3, Vector3> onMouseClickFromTo, Action<Vector3, Vector3> onWaitingForToPosition) {
            return CreateMouseClickFromToAction(0, 1, onMouseClickFromTo, onWaitingForToPosition);
        }

        public static FunctionUpdater CreateMouseClickFromToAction(int mouseButton, int cancelMouseButton, Action<Vector3, Vector3> onMouseClickFromTo, Action<Vector3, Vector3> onWaitingForToPosition) {
            int state = 0;
            Vector3 from = Vector3.zero;
            return FunctionUpdater.Create(() => {
                if (state == 1) {
                    if (onWaitingForToPosition != null) onWaitingForToPosition(from, UtilsClass.GetMouseWorldPosition());
                }
                if (state == 1 && Input.GetMouseButtonDown(cancelMouseButton)) {
                    // Cancel
                    state = 0;
                }
                if (Input.GetMouseButtonDown(mouseButton) && !UtilsClass.IsPointerOverUI()) {
                    if (state == 0) {
                        state = 1;
                        from = UtilsClass.GetMouseWorldPosition();
                    } else {
                        state = 0;
                        onMouseClickFromTo(from, UtilsClass.GetMouseWorldPosition());
                    }
                }
                return false; 
            });
        }

        public static FunctionUpdater CreateMouseClickAction(Action<Vector3> onMouseClick) {
            return CreateMouseClickAction(0, onMouseClick);
        }

        public static FunctionUpdater CreateMouseClickAction(int mouseButton, Action<Vector3> onMouseClick) {
            return FunctionUpdater.Create(() => {
                if (Input.GetMouseButtonDown(mouseButton)) {
                    onMouseClick(GetWorldPositionFromUI());
                }
                return false; 
            });
        }

        public static FunctionUpdater CreateKeyCodeAction(KeyCode keyCode, Action onKeyDown) {
            return FunctionUpdater.Create(() => {
                if (Input.GetKeyDown(keyCode)) {
                    onKeyDown();
                }
                return false; 
            });
        }

        

        // Get UI Position from World Position
        public static Vector2 GetWorldUIPosition(Vector3 worldPosition, Transform parent, Camera uiCamera, Camera worldCamera) {
            Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
            Vector3 uiCameraWorldPosition = uiCamera.ScreenToWorldPoint(screenPosition);
            Vector3 localPos = parent.InverseTransformPoint(uiCameraWorldPosition);
            return new Vector2(localPos.x, localPos.y);
        }

        public static Vector3 GetWorldPositionFromUIZeroZ() {
            Vector3 vec = GetWorldPositionFromUI(Input.mousePosition, Camera.main);
            vec.z = 0f;
            return vec;
        }

        // Get World Position from UI Position
        public static Vector3 GetWorldPositionFromUI() {
            return GetWorldPositionFromUI(Input.mousePosition, Camera.main);
        }

        public static Vector3 GetWorldPositionFromUI(Camera worldCamera) {
            return GetWorldPositionFromUI(Input.mousePosition, worldCamera);
        }

        public static Vector3 GetWorldPositionFromUI(Vector3 screenPosition, Camera worldCamera) {
            Vector3 worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
            return worldPosition;
        }
    
        public static Vector3 GetWorldPositionFromUI_Perspective() {
            return GetWorldPositionFromUI_Perspective(Input.mousePosition, Camera.main);
        }

        public static Vector3 GetWorldPositionFromUI_Perspective(Camera worldCamera) {
            return GetWorldPositionFromUI_Perspective(Input.mousePosition, worldCamera);
        }

        public static Vector3 GetWorldPositionFromUI_Perspective(Vector3 screenPosition, Camera worldCamera) {
            Ray ray = worldCamera.ScreenPointToRay(screenPosition);
            Plane xy = new Plane(Vector3.forward, new Vector3(0, 0, 0f));
            float distance;
            xy.Raycast(ray, out distance);
            return ray.GetPoint(distance);
        }


        // Screen Shake
        public static void ShakeCamera(float intensity, float timer) {
            Vector3 lastCameraMovement = Vector3.zero;
            FunctionUpdater.Create(delegate () {
                timer -= Time.unscaledDeltaTime;
                Vector3 randomMovement = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized * intensity;
                Camera.main.transform.position = Camera.main.transform.position - lastCameraMovement + randomMovement;
                lastCameraMovement = randomMovement;
                return timer <= 0f;
            }, "CAMERA_SHAKE");
        }


        // Trigger an action next frame
        public static FunctionUpdater ActionNextFrame(Action action) {
            return FunctionUpdater.Create(() => {
                action();
                return true;
            });
        }

        // Return random element from array
        public static T GetRandom<T>(T[] array) {
            return array[UnityEngine.Random.Range(0, array.Length)];
        }


        // Return a number with milli dots, 1000000 -> 1.000.000
        public static string GetMilliDots(float n) {
            return GetMilliDots((long)n);
        }

        public static string GetMilliDots(long n) {
            string ret = n.ToString();
            for (int i = 1; i <= Mathf.Floor(ret.Length / 4); i++) {
                ret = ret.Substring(0, ret.Length - i * 3 - (i - 1)) + "." + ret.Substring(ret.Length - i * 3 - (i - 1));
            }
            return ret;
        }


        // Return with milli dots and dollar sign
        public static string GetDollars(float n) {
            return GetDollars((long)n);
        }
        public static string GetDollars(long n) {
            if (n < 0)
                return "-$" + GetMilliDots(Mathf.Abs(n));
            else
                return "$" + GetMilliDots(n);
        }



        [System.Serializable]
        private class JsonDictionary {
            public List<string> keyList = new List<string>();
            public List<string> valueList = new List<string>();
        }

        // Take a Dictionary and return JSON string
        public static string SaveDictionaryJson<TKey, TValue>(Dictionary<TKey, TValue> dictionary) {
            JsonDictionary jsonDictionary = new JsonDictionary();
            foreach (TKey key in dictionary.Keys) {
                jsonDictionary.keyList.Add(JsonUtility.ToJson(key));
                jsonDictionary.valueList.Add(JsonUtility.ToJson(dictionary[key]));
            }
            string saveJson = JsonUtility.ToJson(jsonDictionary);
            return saveJson;
        }

        // Take a JSON string and return Dictionary<T1, T2>
        public static Dictionary<TKey, TValue> LoadDictionaryJson<TKey, TValue>(string saveJson) {
            JsonDictionary jsonDictionary = JsonUtility.FromJson<JsonDictionary>(saveJson);
            Dictionary<TKey, TValue> ret = new Dictionary<TKey, TValue>();
            for (int i = 0; i < jsonDictionary.keyList.Count; i++) {
                TKey key = JsonUtility.FromJson<TKey>(jsonDictionary.keyList[i]);
                TValue value = JsonUtility.FromJson<TValue>(jsonDictionary.valueList[i]);
                ret[key] = value;
            }
            return ret;
        }


        // Split a string into an array based on a Separator
        public static string[] SplitString(string save, string separator) {
            return save.Split(new string[] { separator }, System.StringSplitOptions.None);
        }


        // Destroy all children of this parent
        public static void DestroyChildren(Transform parent) {
            foreach (Transform transform in parent)
                GameObject.Destroy(transform.gameObject);
        }

        // Destroy all children and randomize their names, useful if you want to do a Find() after calling destroy, since they only really get destroyed at the end of the frame
        public static void DestroyChildrenRandomizeNames(Transform parent) {
            foreach (Transform transform in parent) {
                transform.name = "" + UnityEngine.Random.Range(10000, 99999);
                GameObject.Destroy(transform.gameObject);
            }
        }

        // Destroy all children except the ones with these names
        public static void DestroyChildren(Transform parent, params string[] ignoreArr) {
            foreach (Transform transform in parent) {
                if (System.Array.IndexOf(ignoreArr, transform.name) == -1) // Don't ignore
                    GameObject.Destroy(transform.gameObject);
            }
        }


        // Set all parent and all children to this layer
        public static void SetAllChildrenLayer(Transform parent, int layer) {
            parent.gameObject.layer = layer;
            foreach (Transform trans in parent) {
                SetAllChildrenLayer(trans, layer);
            }
        }



        // Returns a random script that can be used to id
        public static string GetIdString() {
            string alphabet = "0123456789abcdefghijklmnopqrstuvxywz";
            string ret = "";
            for (int i = 0; i < 8; i++) {
                ret += alphabet[UnityEngine.Random.Range(0, alphabet.Length)];
            }
            return ret;
        }

        // Returns a random script that can be used to id (bigger alphabet)
        public static string GetIdStringLong() {
            return GetIdStringLong(10);
        }

        // Returns a random script that can be used to id (bigger alphabet)
        public static string GetIdStringLong(int chars) {
            string alphabet = "0123456789abcdefghijklmnopqrstuvxywzABCDEFGHIJKLMNOPQRSTUVXYWZ";
            string ret = "";
            for (int i = 0; i < chars; i++) {
                ret += alphabet[UnityEngine.Random.Range(0, alphabet.Length)];
            }
            return ret;
        }



        // Get a random male name and optionally single letter surname
        public static string GetRandomName(bool withSurname = false) {
            List<string> firstNameList = new List<string>(){"Gabe","Cliff","Tim","Ron","Jon","John","Mike","Seth","Alex","Steve","Chris","Will","Bill","James","Jim",
                                        "Ahmed","Omar","Peter","Pierre","George","Lewis","Lewie","Adam","William","Ali","Eddie","Ed","Dick","Robert","Bob","Rob",
                                        "Neil","Tyson","Carl","Chris","Christopher","Jensen","Gordon","Morgan","Richard","Wen","Wei","Luke","Lucas","Noah","Ivan","Yusuf",
                                        "Ezio","Connor","Milan","Nathan","Victor","Harry","Ben","Charles","Charlie","Jack","Leo","Leonardo","Dylan","Steven","Jeff",
                                        "Alex","Mark","Leon","Oliver","Danny","Liam","Joe","Tom","Thomas","Bruce","Clark","Tyler","Jared","Brad","Jason"};

            if (!withSurname) {
                return firstNameList[UnityEngine.Random.Range(0, firstNameList.Count)];
            } else {
                string alphabet = "ABCDEFGHIJKLMNOPQRSTUVXYWZ";
                return firstNameList[UnityEngine.Random.Range(0, firstNameList.Count)] + " " + alphabet[UnityEngine.Random.Range(0, alphabet.Length)] + ".";
            }
        }




        public static string GetRandomCityName() {
            List<string> cityNameList = new List<string>(){"Alabama","New York","Old York","Bangkok","Lisbon","Vee","Agen","Agon","Ardok","Arbok",
                            "Kobra","House","Noun","Hayar","Salma","Chancellor","Dascomb","Payn","Inglo","Lorr","Ringu",
                            "Brot","Mount Loom","Kip","Chicago","Madrid","London","Gam",
                            "Greenvile","Franklin","Clinton","Springfield","Salem","Fairview","Fairfax","Washington","Madison",
                            "Georgetown","Arlington","Marion","Oxford","Harvard","Valley","Ashland","Burlington","Manchester","Clayton",
                            "Milton","Auburn","Dayton","Lexington","Milford","Riverside","Cleveland","Dover","Hudson","Kingston","Mount Vernon",
                            "Newport","Oakland","Centerville","Winchester","Rotary","Bailey","Saint Mary","Three Waters","Veritas","Chaos","Center",
                            "Millbury","Stockland","Deerstead Hills","Plaintown","Fairchester","Milaire View","Bradton","Glenfield","Kirkmore",
                            "Fortdell","Sharonford","Inglewood","Englecamp","Harrisvania","Bosstead","Brookopolis","Metropolis","Colewood","Willowbury",
                            "Hearthdale","Weelworth","Donnelsfield","Greenline","Greenwich","Clarkswich","Bridgeworth","Normont",
                            "Lynchbrook","Ashbridge","Garfort","Wolfpain","Waterstead","Glenburgh","Fortcroft","Kingsbank","Adamstead","Mistead",
                            "Old Crossing","Crossing","New Agon","New Agen","Old Agon","New Valley","Old Valley","New Kingsbank","Old Kingsbank",
            "New Dover","Old Dover","New Burlington","Shawshank","Old Shawshank","New Shawshank","New Bradton", "Old Bradton","New Metropolis","Old Clayton","New Clayton"
        };
            return cityNameList[UnityEngine.Random.Range(0, cityNameList.Count)];
        }



        // Is this position inside the FOV? Top Down Perspective
        public static bool IsPositionInsideFov(Vector3 pos, Vector3 aimDir, Vector3 posTarget, float fov) {
            int aimAngle = UtilsClass.GetAngleFromVector180(aimDir);
            int angle = UtilsClass.GetAngleFromVector180(posTarget - pos);
            int angleDifference = (angle - aimAngle);
            if (angleDifference > 180) angleDifference -= 360;
            if (angleDifference < -180) angleDifference += 360;
            if (!(angleDifference < fov / 2f && angleDifference > -fov / 2f)) {
                // Not inside fov
                return false;
            } else {
                // Inside fov
                return true;
            }
        }

        // Take two color arrays (pixels) and merge them
        public static void MergeColorArrays(Color[] baseArray, Color[] overlay) {
            for (int i = 0; i < baseArray.Length; i++) {
                if (overlay[i].a > 0) {
                    // Not empty color
                    if (overlay[i].a >= 1) {
                        // Fully replace
                        baseArray[i] = overlay[i];
                    } else {
                        // Interpolate colors
                        float alpha = overlay[i].a;
                        baseArray[i].r += (overlay[i].r - baseArray[i].r) * alpha;
                        baseArray[i].g += (overlay[i].g - baseArray[i].g) * alpha;
                        baseArray[i].b += (overlay[i].b - baseArray[i].b) * alpha;
                        baseArray[i].a += overlay[i].a;
                    }
                }
            }
        }

        // Replace color in baseArray with replaceArray if baseArray[i] != ignoreColor
        public static void ReplaceColorArrays(Color[] baseArray, Color[] replaceArray, Color ignoreColor) {
            for (int i = 0; i < baseArray.Length; i++) {
                if (baseArray[i] != ignoreColor) {
                    baseArray[i] = replaceArray[i];
                }
            }
        }

        public static void MaskColorArrays(Color[] baseArray, Color[] mask) {
            for (int i = 0; i < baseArray.Length; i++) {
                if (baseArray[i].a > 0f) {
                    baseArray[i].a = mask[i].a;
                }
            }
        }

        public static void TintColorArray(Color[] baseArray, Color tint) {
            for (int i = 0; i < baseArray.Length; i++) {
                // Apply tint
                baseArray[i].r = tint.r * baseArray[i].r;
                baseArray[i].g = tint.g * baseArray[i].g;
                baseArray[i].b = tint.b * baseArray[i].b;
            }
        }
        public static void TintColorArrayInsideMask(Color[] baseArray, Color tint, Color[] mask) {
            for (int i = 0; i < baseArray.Length; i++) {
                if (mask[i].a > 0) {
                    // Apply tint
                    Color baseColor = baseArray[i];
                    Color fullyTintedColor = tint * baseColor;
                    float interpolateAmount = mask[i].a;
                    baseArray[i].r = baseColor.r + (fullyTintedColor.r - baseColor.r) * interpolateAmount;
                    baseArray[i].g = baseColor.g + (fullyTintedColor.g - baseColor.g) * interpolateAmount;
                    baseArray[i].b = baseColor.b + (fullyTintedColor.b - baseColor.b) * interpolateAmount;
                }
            }
        }

        public static Color TintColor(Color baseColor, Color tint) {
            // Apply tint
            baseColor.r = tint.r * baseColor.r;
            baseColor.g = tint.g * baseColor.g;
            baseColor.b = tint.b * baseColor.b;

            return baseColor;
        }

        public static bool IsColorSimilar255(Color colorA, Color colorB, int maxDiff) {
            return IsColorSimilar(colorA, colorB, maxDiff / 255f);
        }

        public static bool IsColorSimilar(Color colorA, Color colorB, float maxDiff) {
            float rDiff = Mathf.Abs(colorA.r - colorB.r);
            float gDiff = Mathf.Abs(colorA.g - colorB.g);
            float bDiff = Mathf.Abs(colorA.b - colorB.b);
            float aDiff = Mathf.Abs(colorA.a - colorB.a);

            float totalDiff = rDiff + gDiff + bDiff + aDiff;
            return totalDiff < maxDiff;
        }

        public static float GetColorDifference(Color colorA, Color colorB) {
            float rDiff = Mathf.Abs(colorA.r - colorB.r);
            float gDiff = Mathf.Abs(colorA.g - colorB.g);
            float bDiff = Mathf.Abs(colorA.b - colorB.b);
            float aDiff = Mathf.Abs(colorA.a - colorB.a);

            float totalDiff = rDiff + gDiff + bDiff + aDiff;
            return totalDiff;
        }



        public static Vector3 GetRandomPositionWithinRectangle(float xMin, float xMax, float yMin, float yMax) {
            return new Vector3(UnityEngine.Random.Range(xMin, xMax), UnityEngine.Random.Range(yMin, yMax));
        }

        public static Vector3 GetRandomPositionWithinRectangle(Vector3 lowerLeft, Vector3 upperRight) {
            return new Vector3(UnityEngine.Random.Range(lowerLeft.x, upperRight.x), UnityEngine.Random.Range(lowerLeft.y, upperRight.y));
        }





        public static string GetTimeHMS(float time, bool hours = true, bool minutes = true, bool seconds = true, bool milliseconds = true) {
            string h0, h1, m0, m1, s0, s1, ms0, ms1, ms2;
            GetTimeCharacterStrings(time, out h0, out h1, out m0, out m1, out s0, out s1, out ms0, out ms1, out ms2);
            string h = h0 + h1;
            string m = m0 + m1;
            string s = s0 + s1;
            string ms = ms0 + ms1 + ms2;

            if (hours) {
                if (minutes) {
                    if (seconds) {
                        if (milliseconds) {
                            return h + ":" + m + ":" + s + "." + ms;
                        } else {
                            return h + ":" + m + ":" + s;
                        }
                    } else {
                        return h + ":" + m;
                    }
                } else {
                    return h;
                }
            } else {
                if (minutes) {
                    if (seconds) {
                        if (milliseconds) {
                            return m + ":" + s + "." + ms;
                        } else {
                            return m + ":" + s;
                        }
                    } else {
                        return m;
                    }
                } else {
                    if (seconds) {
                        if (milliseconds) {
                            return s + "." + ms;
                        } else {
                            return s;
                        }
                    } else {
                        return ms;
                    }
                }
            }
        }

        public static void SetupTimeHMSTransform(Transform transform, float time) {
            string h0, h1, m0, m1, s0, s1, ms0, ms1, ms2;
            GetTimeCharacterStrings(time, out h0, out h1, out m0, out m1, out s0, out s1, out ms0, out ms1, out ms2);

            if (transform.Find("h0") != null && transform.Find("h0").GetComponent<Text>() != null)
                transform.Find("h0").GetComponent<Text>().text = h0;
            if (transform.Find("h1") != null && transform.Find("h1").GetComponent<Text>() != null)
                transform.Find("h1").GetComponent<Text>().text = h1;

            if (transform.Find("m0") != null && transform.Find("m0").GetComponent<Text>() != null)
                transform.Find("m0").GetComponent<Text>().text = m0;
            if (transform.Find("m1") != null && transform.Find("m1").GetComponent<Text>() != null)
                transform.Find("m1").GetComponent<Text>().text = m1;

            if (transform.Find("s0") != null && transform.Find("s0").GetComponent<Text>() != null)
                transform.Find("s0").GetComponent<Text>().text = s0;
            if (transform.Find("s1") != null && transform.Find("s1").GetComponent<Text>() != null)
                transform.Find("s1").GetComponent<Text>().text = s1;

            if (transform.Find("ms0") != null && transform.Find("ms0").GetComponent<Text>() != null)
                transform.Find("ms0").GetComponent<Text>().text = ms0;
            if (transform.Find("ms1") != null && transform.Find("ms1").GetComponent<Text>() != null)
                transform.Find("ms1").GetComponent<Text>().text = ms1;
            if (transform.Find("ms2") != null && transform.Find("ms2").GetComponent<Text>() != null)
                transform.Find("ms2").GetComponent<Text>().text = ms2;
        }

        public static void GetTimeHMS(float time, out int h, out int m, out int s, out int ms) {
            s = Mathf.FloorToInt(time);
            m = Mathf.FloorToInt(s / 60f);
            h = Mathf.FloorToInt((s / 60f) / 60f);
            s = s - m * 60;
            m = m - h * 60;
            ms = (int)((time - Mathf.FloorToInt(time)) * 1000);
        }

        public static void GetTimeCharacterStrings(float time, out string h0, out string h1, out string m0, out string m1, out string s0, out string s1, out string ms0, out string ms1, out string ms2) {
            int s = Mathf.FloorToInt(time);
            int m = Mathf.FloorToInt(s / 60f);
            int h = Mathf.FloorToInt((s / 60f) / 60f);
            s = s - m * 60;
            m = m - h * 60;
            int ms = (int)((time - Mathf.FloorToInt(time)) * 1000);

            if (h < 10) {
                h0 = "0";
                h1 = "" + h;
            } else {
                h0 = "" + Mathf.FloorToInt(h / 10f);
                h1 = "" + (h - Mathf.FloorToInt(h / 10f) * 10);
            }

            if (m < 10) {
                m0 = "0";
                m1 = "" + m;
            } else {
                m0 = "" + Mathf.FloorToInt(m / 10f);
                m1 = "" + (m - Mathf.FloorToInt(m / 10f) * 10);
            }

            if (s < 10) {
                s0 = "0";
                s1 = "" + s;
            } else {
                s0 = "" + Mathf.FloorToInt(s / 10f);
                s1 = "" + (s - Mathf.FloorToInt(s / 10f) * 10);
            }


            if (ms < 10) {
                ms0 = "0";
                ms1 = "0";
                ms2 = "" + ms;
            } else {
                // >= 10
                if (ms < 100) {
                    ms0 = "0";
                    ms1 = "" + Mathf.FloorToInt(ms / 10f);
                    ms2 = "" + (ms - Mathf.FloorToInt(ms / 10f) * 10);
                } else {
                    // >= 100
                    int _i_ms0 = Mathf.FloorToInt(ms / 100f);
                    int _i_ms1 = Mathf.FloorToInt(ms / 10f) - (_i_ms0 * 10);
                    int _i_ms2 = ms - (_i_ms1 * 10) - (_i_ms0 * 100);
                    ms0 = "" + _i_ms0;
                    ms1 = "" + _i_ms1;
                    ms2 = "" + _i_ms2;
                }
            }
        }

        public static void PrintTimeMilliseconds(float startTime, string prefix = "") {
            Debug.Log(prefix + GetTimeMilliseconds(startTime));
        }

        public static float GetTimeMilliseconds(float startTime) {
            return (Time.realtimeSinceStartup - startTime) * 1000f;
        }





        public static List<Vector3> GetPositionListAround(Vector3 position, float distance, int positionCount) {
            List<Vector3> ret = new List<Vector3>();
            for (int i = 0; i < positionCount; i++) {
                int angle = i * (360 / positionCount);
                Vector3 dir = UtilsClass.ApplyRotationToVector(new Vector3(0, 1), angle);
                Vector3 pos = position + dir * distance;
                ret.Add(pos);
            }
            return ret;
        }

        public static List<Vector3> GetPositionListAround(Vector3 position, float[] ringDistance, int[] ringPositionCount) {
            List<Vector3> ret = new List<Vector3>();
            for (int ring = 0; ring < ringPositionCount.Length; ring++) {
                List<Vector3> ringPositionList = GetPositionListAround(position, ringDistance[ring], ringPositionCount[ring]);
                ret.AddRange(ringPositionList);
            }
            return ret;
        }

        public static List<Vector3> GetPositionListAround(Vector3 position, float distance, int positionCount, Vector3 direction, int angleStart, int angleIncrease) {
            List<Vector3> ret = new List<Vector3>();
            for (int i = 0; i < positionCount; i++) {
                int angle = angleStart + angleIncrease * i;
                Vector3 dir = UtilsClass.ApplyRotationToVector(direction, angle);
                Vector3 pos = position + dir * distance;
                ret.Add(pos);
            }
            return ret;
        }

        public static List<Vector3> GetPositionListAlongDirection(Vector3 position, Vector3 direction, float distancePerPosition, int positionCount) {
            List<Vector3> ret = new List<Vector3>();
            for (int i = 0; i < positionCount; i++) {
                Vector3 pos = position + direction * (distancePerPosition * i);
                ret.Add(pos);
            }
            return ret;
        }

        public static List<Vector3> GetPositionListAlongAxis(Vector3 positionStart, Vector3 positionEnd, int positionCount) {
            Vector3 direction = (positionEnd - positionStart).normalized;
            float distancePerPosition = (positionEnd - positionStart).magnitude / positionCount;
            return GetPositionListAlongDirection(positionStart + direction * (distancePerPosition / 2f), direction, distancePerPosition, positionCount);
        }

        public static List<Vector3> GetPositionListWithinRect(Vector3 lowerLeft, Vector3 upperRight, int positionCount) {
            List<Vector3> ret = new List<Vector3>();
            float width = upperRight.x - lowerLeft.x;
            float height = upperRight.y - lowerLeft.y;
            float area = width * height;
            float areaPerPosition = area / positionCount;
            float positionSquareSize = Mathf.Sqrt(areaPerPosition);
            Vector3 rowLeft, rowRight;
            rowLeft = new Vector3(lowerLeft.x, lowerLeft.y);
            rowRight = new Vector3(upperRight.x, lowerLeft.y);
            int rowsTotal = Mathf.RoundToInt(height / positionSquareSize);
            float increaseY = height / rowsTotal;
            rowLeft.y += increaseY / 2f;
            rowRight.y += increaseY / 2f;
            int positionsPerRow = Mathf.RoundToInt(width / positionSquareSize);
            for (int i = 0; i < rowsTotal; i++) {
                ret.AddRange(GetPositionListAlongAxis(rowLeft, rowRight, positionsPerRow));
                rowLeft.y += increaseY;
                rowRight.y += increaseY;
            }
            int missingPositions = positionCount - ret.Count;
            Vector3 angleDir = (upperRight - lowerLeft) / missingPositions;
            for (int i = 0; i < missingPositions; i++) {
                ret.Add(lowerLeft + (angleDir / 2f) + angleDir * i);
            }
            while (ret.Count > positionCount) {
                ret.RemoveAt(UnityEngine.Random.Range(0, ret.Count));
            }
            return ret;
        }



        public static List<Vector2Int> GetPosXYListDiamond(int size) {
            List<Vector2Int> list = new List<Vector2Int>();
            for (int x = 0; x < size; x++) {
                for (int y = 0; y < size - x; y++) {
                    list.Add(new Vector2Int(x, y));
                    list.Add(new Vector2Int(-x, y));
                    list.Add(new Vector2Int(x, -y));
                    list.Add(new Vector2Int(-x, -y));
                }
            }
            return list;
        }

        public static List<Vector2Int> GetPosXYListOblong(int width, int dropXamount, int increaseDropXamount, Vector3 dir) {
            List<Vector2Int> list = GetPosXYListOblong(width, dropXamount, increaseDropXamount);
            list = RotatePosXYList(list, UtilsClass.GetAngleFromVector(dir));
            return list;
        }

        public static List<Vector2Int> GetPosXYListOblong(int width, int dropXamount, int increaseDropXamount) {
            List<Vector2Int> triangle = GetPosXYListTriangle(width, dropXamount, increaseDropXamount);
            List<Vector2Int> list = new List<Vector2Int>(triangle);
            foreach (Vector2Int posXY in triangle) {
                if (posXY.y == 0) continue;
                list.Add(new Vector2Int(posXY.x, -posXY.y));
            }
            foreach (Vector2Int posXY in new List<Vector2Int>(list)) {
                if (posXY.x == 0) continue;
                list.Add(new Vector2Int(-posXY.x, posXY.y));
            }
            return list;
        }

        public static List<Vector2Int> GetPosXYListTriangle(int width, int dropXamount, int increaseDropXamount) {
            List<Vector2Int> list = new List<Vector2Int>();
            for (int i = 0; i > -999; i--) {
                for (int j = 0; j < width; j++) {
                    list.Add(new Vector2Int(j, i));
                }
                width -= dropXamount;
                dropXamount += increaseDropXamount;
                if (width <= 0) break;
            }
            return list;
        }

        public static List<Vector2Int> RotatePosXYList(List<Vector2Int> list, int angle) {
            List<Vector2Int> ret = new List<Vector2Int>();
            for (int i = 0; i < list.Count; i++) {
                Vector2Int posXY = list[i];
                Vector3 vec = UtilsClass.ApplyRotationToVector(new Vector3(posXY.x, posXY.y), angle);
                ret.Add(new Vector2Int(Mathf.RoundToInt(vec.x), Mathf.RoundToInt(vec.y)));
            }
            return ret;
        }






        public static Transform CloneTransform(Transform transform, string name = null) {
            Transform clone = GameObject.Instantiate(transform, transform.parent);

            if (name != null)
                clone.name = name;
            else
                clone.name = transform.name;

            return clone;
        }

        public static Transform CloneTransform(Transform transform, string name, Vector2 newAnchoredPosition, bool setActiveTrue = false) {
            Transform clone = CloneTransform(transform, name);
            clone.GetComponent<RectTransform>().anchoredPosition = newAnchoredPosition;
            if (setActiveTrue) {
                clone.gameObject.SetActive(true);
            }
            return clone;
        }

        public static Transform CloneTransform(Transform transform, Transform newParent, string name = null) {
            Transform clone = GameObject.Instantiate(transform, newParent);

            if (name != null)
                clone.name = name;
            else
                clone.name = transform.name;

            return clone;
        }

        public static Transform CloneTransform(Transform transform, Transform newParent, string name, Vector2 newAnchoredPosition, bool setActiveTrue = false) {
            Transform clone = CloneTransform(transform, newParent, name);
            clone.GetComponent<RectTransform>().anchoredPosition = newAnchoredPosition;
            if (setActiveTrue) {
                clone.gameObject.SetActive(true);
            }
            return clone;
        }

        public static Transform CloneTransformWorld(Transform transform, Transform newParent, string name, Vector3 newLocalPosition) {
            Transform clone = CloneTransform(transform, newParent, name);
            clone.localPosition = newLocalPosition;
            return clone;
        }



        public static T[] ArrayAdd<T>(T[] arr, T add) {
            T[] ret = new T[arr.Length + 1];
            for (int i = 0; i < arr.Length; i++) {
                ret[i] = arr[i];
            }
            ret[arr.Length] = add;
            return ret;
        }

        public static void ShuffleArray<T>(T[] arr, int iterations) {
            for (int i = 0; i < iterations; i++) {
                int rnd = UnityEngine.Random.Range(0, arr.Length);
                T tmp = arr[rnd];
                arr[rnd] = arr[0];
                arr[0] = tmp;
            }
        }
        public static void ShuffleArray<T>(T[] arr, int iterations, System.Random random) {
            for (int i = 0; i < iterations; i++) {
                int rnd = random.Next(0, arr.Length);
                T tmp = arr[rnd];
                arr[rnd] = arr[0];
                arr[0] = tmp;
            }
        }

        public static void ShuffleList<T>(List<T> list, int iterations) {
            for (int i = 0; i < iterations; i++) {
                int rnd = UnityEngine.Random.Range(0, list.Count);
                T tmp = list[rnd];
                list[rnd] = list[0];
                list[0] = tmp;
            }
        }


        public static void DebugDrawCircle(Vector3 center, float radius, Color color, float duration, int divisions) {
            for (int i = 0; i <= divisions; i++) {
                Vector3 vec1 = center + UtilsClass.ApplyRotationToVector(new Vector3(0, 1) * radius, (360f / divisions) * i);
                Vector3 vec2 = center + UtilsClass.ApplyRotationToVector(new Vector3(0, 1) * radius, (360f / divisions) * (i + 1));
                Debug.DrawLine(vec1, vec2, color, duration);
            }
        }

        public static void DebugDrawRectangle(Vector3 minXY, Vector3 maxXY, Color color, float duration) {
            Debug.DrawLine(new Vector3(minXY.x, minXY.y), new Vector3(maxXY.x, minXY.y), color, duration);
            Debug.DrawLine(new Vector3(minXY.x, minXY.y), new Vector3(minXY.x, maxXY.y), color, duration);
            Debug.DrawLine(new Vector3(minXY.x, maxXY.y), new Vector3(maxXY.x, maxXY.y), color, duration);
            Debug.DrawLine(new Vector3(maxXY.x, minXY.y), new Vector3(maxXY.x, maxXY.y), color, duration);
        }

        public static void DebugDrawText(string text, Vector3 position, Color color, float size, float duration) {
            text = text.ToUpper();
            float kerningSize = size * 0.6f;
            Vector3 basePosition = position;
            for (int i = 0; i < text.Length; i++) {
                char c = text[i];
                switch (c) {
                    case '\n':
                        // Newline
                        position.x = basePosition.x;
                        position.y += size;
                        break;
                    case ' ':
                        position.x += kerningSize;
                        break;
                    default:
                        DebugDrawChar(c, position, color, size, duration);
                        position.x += kerningSize;
                        break;
                }
            }
        }

        // Draw Characters using Debug DrawLine Gizmos
        public static void DebugDrawChar(char c, Vector3 position, Color color, float size, float duration) {
            switch (c) {
                default:
                case 'A':
                    DebugDrawLines(position, color, size, duration, new[] {
                0.317f,0.041f, 0.5f,0.98f, 0.749f,0.062f, 0.625f,0.501f, 0.408f,0.507f }); break;
                case 'B':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.289f,0.069f, 0.274f,0.937f, 0.609f,0.937f, 0.801f,0.879f, 0.829f,0.708f, 0.756f,0.538f, 0.655f,0.492f, 0.442f,0.495f, 0.271f,0.495f, 0.567f,0.474f, 0.676f,0.465f, 0.722f,0.385f, 0.719f,0.181f, 0.664f,0.087f, 0.527f,0.053f, 0.396f,0.05f, 0.271f,0.078f }); break;
                case 'C':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.695f,0.946f, 0.561f,0.949f, 0.426f,0.937f, 0.317f,0.867f, 0.265f,0.733f, 0.262f,0.553f, 0.292f,0.27f, 0.323f,0.172f, 0.417f,0.12f, 0.512f,0.096f, 0.637f,0.093f, 0.743f,0.117f, }); break;
                case 'D':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.314f,0.909f, 0.329f,0.096f, 0.53f,0.123f, 0.594f,0.197f, 0.673f,0.334f, 0.716f,0.498f, 0.692f,0.666f, 0.609f,0.806f, 0.457f,0.891f, 0.323f,0.919f }); break;
                case 'E':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.344f,0.919f, 0.363f,0.078f, 0.713f,0.096f, 0.359f,0.096f, 0.347f,0.48f, 0.53f,0.492f, 0.356f,0.489f, 0.338f,0.913f, 0.625f,0.919f }); break;
                case 'F':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.682f,0.916f, 0.329f,0.909f, 0.341f,0.66f, 0.503f,0.669f, 0.341f,0.669f, 0.317f,0.087f }); break;
                case 'G':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.618f,0.867f, 0.399f,0.849f, 0.292f,0.654f, 0.241f,0.404f, 0.253f,0.178f, 0.481f,0.075f, 0.612f,0.078f, 0.725f,0.169f, 0.728f,0.334f, 0.71f,0.437f, 0.609f,0.462f, 0.463f,0.462f }); break;
                case 'H':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.277f,0.876f, 0.305f,0.133f, 0.295f,0.507f, 0.628f,0.501f, 0.643f,0.139f, 0.637f,0.873f }); break;
                case 'I':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.487f,0.906f, 0.484f,0.096f }); break;
                case 'J':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.628f,0.882f, 0.679f,0.242f, 0.603f,0.114f, 0.445f,0.066f, 0.317f,0.114f, 0.262f,0.209f, 0.253f,0.3f, 0.259f,0.367f }); break;
                case 'K':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.292f,0.879f, 0.311f,0.111f, 0.305f,0.498f, 0.594f,0.876f, 0.305f,0.516f, 0.573f,0.154f,  }); break;
                case 'L':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.311f,0.879f, 0.308f,0.133f, 0.682f,0.148f,  }); break;
                case 'M':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.262f,0.12f, 0.265f,0.909f, 0.509f,0.608f, 0.71f,0.919f, 0.713f,0.151f,  }); break;
                case 'N':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.737f,0.885f, 0.679f,0.114f, 0.335f,0.845f, 0.353f,0.175f,  }); break;
                case 'O':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.609f,0.906f, 0.396f,0.894f, 0.271f,0.687f, 0.232f,0.474f, 0.241f,0.282f, 0.356f,0.142f, 0.527f,0.087f, 0.655f,0.09f, 0.719f,0.181f, 0.737f,0.379f, 0.737f,0.638f, 0.71f,0.836f, 0.628f,0.919f, 0.582f,0.919f, }); break;
                case 'P':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.314f,0.129f, 0.311f,0.873f, 0.658f,0.906f, 0.746f,0.8f, 0.746f,0.66f, 0.673f,0.544f, 0.509f,0.51f, 0.359f,0.51f, 0.311f,0.516f,  }); break;
                case 'Q':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.497f,0.925f, 0.335f,0.894f, 0.228f,0.681f, 0.213f,0.379f, 0.25f,0.145f, 0.396f,0.096f, 0.573f,0.105f, 0.631f,0.166f, 0.542f,0.245f, 0.752f,0.108f, 0.628f,0.187f, 0.685f,0.261f, 0.728f,0.398f, 0.759f,0.605f, 0.722f,0.794f, 0.64f,0.916f, 0.475f,0.946f,  }); break;
                case 'R':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.347f,0.142f, 0.332f,0.9f, 0.667f,0.897f, 0.698f,0.699f, 0.655f,0.58f, 0.521f,0.553f, 0.396f,0.553f, 0.344f,0.553f, 0.564f,0.37f, 0.655f,0.206f, 0.71f,0.169f }); break;
                case 'S':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.695f,0.842f, 0.576f,0.882f, 0.439f,0.885f, 0.329f,0.8f, 0.289f,0.626f, 0.317f,0.489f, 0.439f,0.44f, 0.621f,0.434f, 0.695f,0.358f, 0.713f,0.224f, 0.646f,0.111f, 0.494f,0.093f, 0.338f,0.105f, 0.289f,0.151f, }); break;
                case 'T':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.497f,0.172f, 0.5f,0.864f, 0.286f,0.858f, 0.719f,0.852f,  }); break;
                case 'U':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.232f,0.858f, 0.247f,0.251f, 0.366f,0.105f, 0.466f,0.078f, 0.615f,0.084f, 0.704f,0.123f, 0.746f,0.276f, 0.74f,0.559f, 0.737f,0.806f, 0.722f,0.864f, }); break;
                case 'V':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.238f,0.855f, 0.494f,0.105f, 0.707f,0.855f,  }); break;
                case 'X':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.783f,0.852f, 0.256f,0.133f, 0.503f,0.498f, 0.305f,0.824f, 0.789f,0.117f,  }); break;
                case 'Y':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.299f,0.842f, 0.497f,0.529f, 0.646f,0.842f, 0.49f,0.541f, 0.487f,0.105f, }); break;
                case 'W':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.228f,0.815f, 0.381f,0.093f, 0.503f,0.434f, 0.615f,0.151f, 0.722f,0.818f,  }); break;
                case 'Z':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.25f,0.87f, 0.795f,0.842f, 0.274f,0.133f, 0.716f,0.142f }); break;


                case '0':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.536f,0.891f, 0.509f,0.891f, 0.42f,0.809f, 0.378f,0.523f, 0.372f,0.215f, 0.448f,0.087f, 0.539f,0.069f, 0.609f,0.099f, 0.637f,0.242f, 0.646f,0.416f, 0.646f,0.608f, 0.631f,0.809f, 0.554f,0.888f, 0.527f,0.894f,  }); break;
                case '1':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.652f,0.108f, 0.341f,0.114f, 0.497f,0.12f, 0.497f,0.855f, 0.378f,0.623f,  }); break;
                case '2':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.311f,0.714f, 0.375f,0.83f, 0.564f,0.894f, 0.722f,0.839f, 0.765f,0.681f, 0.634f,0.483f, 0.5f,0.331f, 0.366f,0.245f, 0.299f,0.126f, 0.426f,0.126f, 0.621f,0.136f, 0.679f,0.136f, 0.737f,0.139f,  }); break;
                case '3':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.289f,0.855f, 0.454f,0.876f, 0.606f,0.818f, 0.685f,0.702f, 0.664f,0.547f, 0.564f,0.459f, 0.484f,0.449f, 0.417f,0.455f, 0.53f,0.434f, 0.655f,0.355f, 0.664f,0.233f, 0.591f,0.105f, 0.466f,0.075f, 0.335f,0.084f, 0.259f,0.142f,  }); break;
                case '4':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.353f,0.836f, 0.262f,0.349f, 0.579f,0.367f, 0.5f,0.376f, 0.49f,0.471f, 0.509f,0.069f,  }); break;
                case '5':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.67f,0.852f, 0.335f,0.858f, 0.347f,0.596f, 0.582f,0.602f, 0.698f,0.513f, 0.749f,0.343f, 0.719f,0.187f, 0.561f,0.133f, 0.363f,0.151f,  }); break;
                case '6':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.567f,0.888f, 0.442f,0.782f, 0.35f,0.544f, 0.326f,0.288f, 0.39f,0.157f, 0.615f,0.142f, 0.679f,0.245f, 0.676f,0.37f, 0.573f,0.48f, 0.454f,0.48f, 0.378f,0.41f, 0.335f,0.367f,  }); break;
                case '7':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.286f,0.852f, 0.731f,0.864f, 0.417f,0.117f, 0.57f,0.498f, 0.451f,0.483f, 0.688f,0.501f, }); break;
                case '8':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.518f,0.541f, 0.603f,0.623f, 0.649f,0.748f, 0.612f,0.858f, 0.497f,0.888f, 0.375f,0.824f, 0.341f,0.708f, 0.381f,0.611f, 0.494f,0.55f, 0.557f,0.513f, 0.6f,0.416f, 0.631f,0.312f, 0.579f,0.178f, 0.509f,0.108f, 0.436f,0.102f, 0.335f,0.181f, 0.308f,0.279f, 0.347f,0.401f, 0.423f,0.486f, 0.497f,0.547f,  }); break;
                case '9':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.475f,0.129f, 0.573f,0.495f, 0.646f,0.824f, 0.509f,0.97f, 0.28f,0.94f, 0.189f,0.827f, 0.262f,0.708f, 0.396f,0.69f, 0.564f,0.745f, 0.646f,0.83f,  }); break;


                case '.':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.515f,0.157f, 0.469f,0.148f, 0.469f,0.117f, 0.515f,0.123f, 0.503f,0.169f }); break;
                case ':':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.515f,0.157f, 0.469f,0.148f, 0.469f,0.117f, 0.515f,0.123f, 0.503f,0.169f });
                    DebugDrawLines(position, color, size, duration, new[] {
                0.515f,.5f+0.157f, 0.469f,.5f+0.148f, 0.469f,.5f+0.117f, 0.515f,.5f+0.123f, 0.503f,.5f+0.169f });
                    break;
                case '-':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.277f,0.51f, 0.716f,0.51f,  }); break;
                case '+':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.265f,0.513f, 0.676f,0.516f, 0.497f,0.529f, 0.49f,0.699f, 0.497f,0.27f,  }); break;

                case '(':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.542f,0.934f, 0.411f,0.797f, 0.344f,0.587f, 0.341f,0.434f, 0.375f,0.257f, 0.457f,0.12f, 0.567f,0.075f, }); break;
                case ')':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.442f,0.94f, 0.548f,0.757f, 0.625f,0.568f, 0.64f,0.392f, 0.554f,0.129f, 0.472f,0.056f,  }); break;
                case ';':
                case ',':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.533f,0.239f, 0.527f,0.154f, 0.487f,0.099f, 0.451f,0.062f,  }); break;
                case '_':
                    DebugDrawLines(position, color, size, duration, new[] {
               0.274f,0.133f, 0.716f,0.142f }); break;

                    /*
             case ':': DebugDrawLines(position, color, size, duration, new [] {
                    0f }); break;
                    */
            }
        }

        public static void DebugDrawLines(Vector3 position, Color color, float size, float duration, Vector3[] points) {
            for (int i = 0; i < points.Length - 1; i++) {
                Debug.DrawLine(position + points[i] * size, position + points[i + 1] * size, color, duration);
            }
        }

        public static void DebugDrawLines(Vector3 position, Color color, float size, float duration, float[] points) {
            List<Vector3> vecList = new List<Vector3>();
            for (int i = 0; i < points.Length; i += 2) {
                Vector3 vec = new Vector3(points[i + 0], points[i + 1]);
                vecList.Add(vec);
            }
            DebugDrawLines(position, color, size, duration, vecList.ToArray());
        }

        public static void ClearLogConsole() {
#if UNITY_EDITOR
            //Debug.Log("################# DISABLED BECAUSE OF BUILD!");
            /*
            Assembly assembly = Assembly.GetAssembly(typeof(UnityEditor.SceneView));
            System.Type logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
            MethodInfo clearConsoleMethod = logEntries.GetMethod("Clear");
            clearConsoleMethod.Invoke(new object(), null);
            //*/
#endif
        }


        public static string GetPercentString(float f, bool includeSign = true) {
            return Mathf.RoundToInt(f * 100f) + (includeSign ? "%" : "");
        }



        public static string GetMonthName(int month) {
            switch (month) {
                default:
                case 0: return "January";
                case 1: return "February";
                case 2: return "March";
                case 3: return "April";
                case 4: return "May";
                case 5: return "June";
                case 6: return "July";
                case 7: return "August";
                case 8: return "September";
                case 9: return "October";
                case 10: return "November";
                case 11: return "December";
            }
        }

        public static string GetMonthNameShort(int month) {
            return GetMonthName(month).Substring(0, 3);
        }




        public static class ReflectionTools {

            public static object CallMethod(string typeName, string methodName) {
                return System.Type.GetType(typeName).GetMethod(methodName).Invoke(null, null);
            }
            public static object GetField(string typeName, string fieldName) {
                System.Reflection.FieldInfo fieldInfo = System.Type.GetType(typeName).GetField(fieldName);
                return fieldInfo.GetValue(null);
            }
            public static System.Type GetNestedType(string typeName, string nestedTypeName) {
                return System.Type.GetType(typeName).GetNestedType(nestedTypeName);
            }

        }



        public static bool TestChance(int chance, int chanceMax = 100) {
            return UnityEngine.Random.Range(0, chanceMax) < chance;
        }

        public static T[] RemoveDuplicates<T>(T[] arr) {
            List<T> list = new List<T>();
            foreach (T t in arr) {
                if (!list.Contains(t)) {
                    list.Add(t);
                }
            }
            return list.ToArray();
        }

        public static List<T> RemoveDuplicates<T>(List<T> arr) {
            List<T> list = new List<T>();
            foreach (T t in arr) {
                if (!list.Contains(t)) {
                    list.Add(t);
                }
            }
            return list;
        }


    }

}