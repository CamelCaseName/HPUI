using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace HPUI
{

    //adapted and simplified from UniverseLib https://github.com/sinai-dev/UniverseLib/blob/main/src/UI/UIFactory.cs
    //originally by SinaiDev, reused under lgpl2.1 now 3
    public static class UIBuilder
    {
        internal static Vector2 largeElementSize = new(100, 30);
        internal static Vector2 smallElementSize = new(25, 25);
        internal static Color defaultTextColor = Color.white;

        public static void SetPosition(this MonoBehaviour behaviour, Vector2 pos)
        {
            var rect = behaviour.GetComponent<RectTransform>();
            rect.localPosition = new Vector3(0, 0, 0);
            rect.sizeDelta = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
            rect.anchoredPosition = pos;
        }

        public static void CreateLayerDropDown(GameObject parent, GameObject obj)
        {
            var propertyGO = CreateUIObject(obj.name + " layer container", parent);
            _ = SetLayoutGroup<HorizontalLayoutGroup>(propertyGO, true, true, padTop: 2, padLeft: 2, padRight: 2, padBottom: 2);

            Dropdown dropDown = null!;
            _ = CreateDropdown(propertyGO, obj.name + " layer dropDown", out dropDown, "Default       ", 14, (int index) =>
            {
                obj.layer = index;
            });

            for (int i = 0; i < 32; i++)
            {
                dropDown.options.Add(new(LayerMask.LayerToName(i)));
            }

            dropDown.value = obj.layer;

            _ = CreateLabel(propertyGO, obj.name + " layer name", obj.name + " layer");
            dropDown.RefreshShownValue();
        }

        public static void CreateLayerMaskDropDown(string propertyName, GameObject parent, UnityEngine.Object obj, PropertyInfo property)
        {

            var propertyGO = CreateUIObject(propertyName + " container", parent);
            _ = SetLayoutGroup<HorizontalLayoutGroup>(propertyGO, true, true, padTop: 2, padLeft: 2, padRight: 2, padBottom: 2);

            Dropdown dropDown = null!;
            Toggle toggle = null!;
            _ = CreateDropdown(propertyGO, propertyName + " dropDown", out dropDown, "Default        ", 14, (int index) =>
            {
                int mask = (int)property.GetValue(obj, null)!;

                toggle.SetIsOnWithoutNotify((mask & (1 << index)) > 0);
            });

            for (int i = 0; i < 32; i++)
            {
                dropDown.options.Add(new(LayerMask.LayerToName(i)));
            }

            dropDown.value = 0;

            _ = CreateLabel(propertyGO, propertyName + " name", propertyName);

            _ = CreateToggle(propertyGO, "enabled", out toggle, out _);

            toggle.onValueChanged.AddListener(new Action<bool>((bool b) =>
            {
                int mask = (int)property.GetValue(obj, null)!;
                if (b)
                {
                    property.SetValue(obj, mask | (1 << dropDown.value));
                }
                else
                {
                    property.SetValue(obj, mask & ~(1 << dropDown.value));
                }
            }));

            dropDown.RefreshShownValue();
        }

        public static void CreateEnumDropDown(string propertyName, GameObject parent, object obj, PropertyInfo property)
        {
            if (property.PropertyType.IsEnum)
            {
                //change how it works between flag enums and normal enums, normal enum we just make a dropdown but for the other one we can go though all and then set the checkbox next to it
                var propertyGO = CreateUIObject(propertyName + " container", parent);
                _ = SetLayoutGroup<HorizontalLayoutGroup>(propertyGO, true, true, 0, 2, 2, 2, 2);

                if (property.PropertyType.IsDefined(typeof(FlagsAttribute), false))
                {
                    Dropdown dropDown = null!;
                    Toggle toggle = null!;
                    _ = CreateDropdown(propertyGO, propertyName + " dropDown", out dropDown, propertyName, 14, (int index) =>
                    {
                        if (Enum.TryParse(property.PropertyType, dropDown.options[index].text, out var enumValue))
                            property.SetValue(obj, enumValue);
                    });

                    int i = 0;
                    int indexToSelect = 0;
                    string currentSelectedValue = property.GetValue(obj, null)!.ToString()!;
                    foreach (var name in Enum.GetNames(property.PropertyType))
                    {
                        if (name == currentSelectedValue)
                            indexToSelect = i;
                        dropDown.options.Add(new(name));
                        ++i;
                    }

                    dropDown.value = indexToSelect;

                    _ = CreateLabel(propertyGO, propertyName + " name", propertyName);

                    _ = CreateToggle(propertyGO, "enabled", out toggle, out _);

                    toggle.onValueChanged.AddListener(new Action<bool>((bool b) =>
                    {
                        int mask = (int)property.GetValue(obj, null)!;
                        if (b)
                        {
                            property.SetValue(obj, mask | (int)Enum.Parse(property.PropertyType, dropDown.itemText.text));
                        }
                        else
                        {
                            property.SetValue(obj, mask & ~(int)Enum.Parse(property.PropertyType, dropDown.itemText.text));
                        }
                    }));

                    dropDown.RefreshShownValue();
                }
                else
                {
                    Dropdown dropDown = null!;
                    _ = CreateDropdown(propertyGO, propertyName + " dropDown", out dropDown, propertyName, 14, (int index) =>
                    {
                        if (Enum.TryParse(property.PropertyType, dropDown.options[index].text, out var enumValue))
                            property.SetValue(obj, enumValue);
                    });

                    int i = 0;
                    int indexToSelect = 0;
                    string currentSelectedValue = property.GetValue(obj, null)!.ToString()!;
                    foreach (var name in Enum.GetNames(property.PropertyType))
                    {
                        if (name == currentSelectedValue)
                            indexToSelect = i;
                        dropDown.options.Add(new(name));
                        ++i;
                    }

                    dropDown.value = indexToSelect;
                    _ = CreateLabel(propertyGO, propertyName + " name", propertyName);

                    dropDown.RefreshShownValue();
                }
            }
        }

        public static void CreateInputField(string propertyName, GameObject parent, object obj)
        {
            var property = obj.GetType().GetProperty(propertyName) ?? throw new InvalidOperationException("the property " + propertyName + " does not exist on " + obj.GetType());

            if (property.PropertyType == typeof(Vector3) || property.PropertyType == typeof(Vector2))
            {
                CreateVectorInputField(propertyName, parent, obj, property);
            }
            else if (property.PropertyType == typeof(bool))
            {
                _ = CreateToggle(parent, propertyName, out Toggle toggle, out _);
                toggle.SetIsOnWithoutNotify((bool)property.GetValue(obj)!);
                toggle.onValueChanged.AddListener(new Action<bool>((bool v) => property.SetValue(obj, v)));
            }
            else
            {
                var propertyGO = CreateUIObject(property.Name + " container", parent);
                _ = SetLayoutGroup<HorizontalLayoutGroup>(propertyGO, true, true, 0, 2, 2, 2, 2);
                InputField input = CreateInputField(propertyGO, property.Name, property.GetValue(obj)?.ToString()!).GetComponent<InputField>();
                input.contentType = property.PropertyType == typeof(float) ? InputField.ContentType.DecimalNumber
                    : property.PropertyType == typeof(int) ? InputField.ContentType.IntegerNumber
                    : property.PropertyType == typeof(string) ? InputField.ContentType.Standard
                    : throw new NotSupportedException("other types like " + property.PropertyType + " are not supported");
                input.characterValidation = property.PropertyType == typeof(float) ? InputField.CharacterValidation.Decimal
                    : property.PropertyType == typeof(int) ? InputField.CharacterValidation.Integer
                    : property.PropertyType == typeof(string) ? InputField.CharacterValidation.None
                    : throw new NotSupportedException("other types like " + property.PropertyType + " are not supported");
                input.onSubmit.AddListener(new Action<string>((string s) => property.SetValue(obj,
                    property.PropertyType == typeof(float) ? float.Parse(s)
                    : property.PropertyType == typeof(int) ? int.Parse(s)
                    : property.PropertyType == typeof(string) ? s
                    : throw new System.NotSupportedException("other types like " + property.PropertyType + " are not supported"))));
                _ = CreateLabel(propertyGO, property.Name + " name", " " + property.Name);
            }
        }

        public static void CreateVectorInputField(string propertyName, GameObject parent, object obj, PropertyInfo property)
        {
            var vector = CreateUIObject(propertyName + " vector container", parent);
            _ = SetLayoutGroup<VerticalLayoutGroup>(vector, true, true, 0, 2, 2, 2, 2);

            var vectorType = property.PropertyType;
            var propertyx = vectorType.GetField("x");
            var propertyy = vectorType.GetField("y");
            var propertyz = vectorType == typeof(Vector3) ? vectorType.GetField("z") : null!;
            object? vectorObj = property.GetValue(obj);

            if (propertyx is null || propertyy is null)
            {
                throw new InvalidOperationException("the vector " + propertyName + " does not contain x or y on " + obj.GetType());
            }

            var propertyXGO = CreateUIObject(property.Name + "X container", vector);
            _ = SetLayoutGroup<HorizontalLayoutGroup>(propertyXGO, true, true, 0, 2, 2, 2, 2);
            InputField inputx = CreateInputField(propertyXGO, property.Name + "X", propertyx.GetValue(vectorObj)?.ToString()!).GetComponent<InputField>();
            inputx.contentType = InputField.ContentType.DecimalNumber;
            inputx.characterValidation = InputField.CharacterValidation.Decimal;
            inputx.onSubmit.AddListener(new Action<string>((string s) => propertyx.SetValue(vectorObj, float.Parse(s))));
            _ = CreateLabel(propertyXGO, property.Name + "X name", " " + property.Name + "X");

            var propertyYGO = CreateUIObject(property.Name + "Y container", vector);
            _ = SetLayoutGroup<HorizontalLayoutGroup>(propertyYGO, true, true, 0, 2, 2, 2, 2);
            InputField inputy = CreateInputField(propertyYGO, property.Name + "Y", propertyy.GetValue(vectorObj)?.ToString()!).GetComponent<InputField>();
            inputy.contentType = InputField.ContentType.DecimalNumber;
            inputy.characterValidation = InputField.CharacterValidation.Decimal;
            inputy.onSubmit.AddListener(new Action<string>((string s) => propertyy.SetValue(vectorObj, float.Parse(s))));
            _ = CreateLabel(propertyYGO, property.Name + "Y name", " " + property.Name + "Y");

            if (propertyz is not null)
            {
                var propertyZGO = CreateUIObject(property.Name + "Z container", vector);
                _ = SetLayoutGroup<HorizontalLayoutGroup>(propertyZGO, true, true, 0, 2, 2, 2, 2);
                InputField inputz = CreateInputField(propertyZGO, property.Name + "Z", propertyz.GetValue(vectorObj)?.ToString()!).GetComponent<InputField>();
                inputz.contentType = InputField.ContentType.DecimalNumber;
                inputz.characterValidation = InputField.CharacterValidation.Decimal;
                inputz.onSubmit.AddListener(new Action<string>((string s) => propertyz.SetValue(vectorObj, float.Parse(s))));
                _ = CreateLabel(propertyZGO, property.Name + "Z name", " " + property.Name + "Z");
            }
        }

        /// <summary>
        /// Create a simple UI object with a RectTransform. <paramref name="parent"/> can be null.
        /// </summary>
        public static GameObject CreateUIObject(string name, GameObject parent, Vector2 sizeDelta = default)
        {
            GameObject obj = new(name)
            {
                layer = 5,
                hideFlags = HideFlags.HideAndDontSave,
            };

            if (parent)
                obj.transform.SetParent(parent.transform, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = sizeDelta;
            return obj;
        }

        public static void SetDefaultTextValues(Text text)
        {
            text.color = defaultTextColor;
            text.font = Font.GetDefault();
            text.fontSize = 14;
        }

        public static void SetDefaultSelectableValues(Selectable selectable)
        {
            Navigation nav = selectable.navigation;
            nav.mode = Navigation.Mode.Explicit;
            selectable.navigation = nav;
        }

        #region Layout Helpers

        /// <summary>
        /// Get and/or Add a LayoutElement component to the GameObject, and set any of the values on it.
        /// </summary>
        public static LayoutElement SetLayoutElement(GameObject gameObject, int? minWidth = null, int? minHeight = null,
            int? flexibleWidth = null, int? flexibleHeight = null, int? preferredWidth = null, int? preferredHeight = null,
            bool? ignoreLayout = null)
        {
            LayoutElement layout = gameObject.GetComponent<LayoutElement>();
            if (!layout)
                layout = gameObject.AddComponent<LayoutElement>();

            if (minWidth != null)
                layout.minWidth = (int)minWidth;

            if (minHeight != null)
                layout.minHeight = (int)minHeight;

            if (flexibleWidth != null)
                layout.flexibleWidth = (int)flexibleWidth;

            if (flexibleHeight != null)
                layout.flexibleHeight = (int)flexibleHeight;

            if (preferredWidth != null)
                layout.preferredWidth = (int)preferredWidth;

            if (preferredHeight != null)
                layout.preferredHeight = (int)preferredHeight;

            if (ignoreLayout != null)
                layout.ignoreLayout = (bool)ignoreLayout;

            return layout;
        }

        /// <summary>
        /// Get and/or Add a HorizontalOrVerticalLayoutGroup (must pick one) to the GameObject, and set the values on it.
        /// </summary>
        public static T SetLayoutGroup<T>(GameObject gameObject, bool? forceWidth = null, bool? forceHeight = null,
            int? spacing = null, int? padTop = null, int? padBottom = null, int? padLeft = null,
            int? padRight = null, TextAnchor? childAlignment = null)
            where T : HorizontalOrVerticalLayoutGroup
        {
            T group = gameObject.GetComponent<T>();
            if (!group)
                group = gameObject.AddComponent<T>();

            return SetLayoutGroup(group, forceWidth, forceHeight, spacing, padTop, padBottom, padLeft,
                padRight, childAlignment);
        }

        /// <summary>
        /// Set the values on a HorizontalOrVerticalLayoutGroup.
        /// </summary>
        public static T SetLayoutGroup<T>(T group, bool? forceWidth = null, bool? forceHeight = null,
            int? spacing = null, int? padTop = null, int? padBottom = null, int? padLeft = null,
            int? padRight = null, TextAnchor? childAlignment = null)
            where T : HorizontalOrVerticalLayoutGroup
        {
            if (forceWidth != null)
                group.childForceExpandWidth = (bool)forceWidth;
            if (forceHeight != null)
                group.childForceExpandHeight = (bool)forceHeight;
            if (spacing != null)
                group.spacing = (int)spacing;
            if (padTop != null)
                group.padding.top = (int)padTop;
            if (padBottom != null)
                group.padding.bottom = (int)padBottom;
            if (padLeft != null)
                group.padding.left = (int)padLeft;
            if (padRight != null)
                group.padding.right = (int)padRight;
            if (childAlignment != null)
                group.childAlignment = (TextAnchor)childAlignment;

            return group;
        }

        #endregion

        #region Layout Objects

        /// <summary>
        /// Create a simple UI Object with a VerticalLayoutGroup and an Image component.
        /// <br /><br />See also: <see cref="PanelBase"/>
        /// </summary>
        /// <param name="name">The name of the panel GameObject, useful for debugging purposes</param>
        /// <param name="parent">The parent GameObject to attach this to</param>
        /// <param name="bgColor">The background color of your panel. Defaults to dark grey if null.</param>
        /// <param name="contentHolder">The GameObject which you should add your actual content on to.</param>
        /// <returns>The base panel GameObject (not for adding content to).</returns>
        public static GameObject CreatePanel(string name, GameObject parent, Vector2 SizeRel, Vector2 position, out GameObject contentHolder, Color? bgColor = null)
        {
            GameObject panelObj = CreateUIObject(name, parent);
            SetLayoutGroup<VerticalLayoutGroup>(panelObj, true, true, 0, 1, 1, 1, 1, TextAnchor.UpperLeft);

            RectTransform rect = panelObj.GetComponent<RectTransform>();
            rect.anchorMin = new(0, 0);
            rect.anchorMax = new(SizeRel.x, SizeRel.y);
            rect.anchoredPosition = position;
            rect.sizeDelta = Vector2.zero;

            panelObj.AddComponent<Image>().color = Color.black;
            panelObj.AddComponent<RectMask2D>();

            contentHolder = CreateUIObject("Content", panelObj);

            Image bgImage = contentHolder.AddComponent<Image>();
            bgImage.type = Image.Type.Filled;
            bgImage.color = bgColor == null
                ? new(0.07f, 0.07f, 0.07f)
                : (Color)bgColor;

            SetLayoutGroup<VerticalLayoutGroup>(contentHolder, true, true, 3, 3, 3, 3, 3);

            return panelObj;
        }

        /// <summary>
        /// Create a VerticalLayoutGroup object with an Image component. Use SetLayoutGroup to create one without an image.
        /// </summary>
        public static GameObject CreateVerticalGroup(GameObject parent, string name, bool forceWidth, bool forceHeight,
            int spacing = 0, Vector4 padding = default, Color bgColor = default, TextAnchor? childAlignment = null)
        {
            GameObject groupObj = CreateUIObject(name, parent);

            SetLayoutGroup<HorizontalLayoutGroup>(groupObj, forceWidth, forceHeight, spacing, (int)padding.x,
                (int)padding.y, (int)padding.z, (int)padding.w, childAlignment);

            Image image = groupObj.AddComponent<Image>();
            image.color = bgColor == default
                            ? new Color(0.17f, 0.17f, 0.17f)
                            : bgColor;

            return groupObj;
        }

        /// <summary>
        /// Create a HorizontalLayoutGroup object with an Image component. Use SetLayoutGroup to create one without an image.
        /// </summary>
        public static GameObject CreateHorizontalGroup(GameObject parent, string name, bool forceExpandWidth, bool forceExpandHeight,
            int spacing = 0, Vector4 padding = default, Color bgColor = default, TextAnchor? childAlignment = null)
        {
            GameObject groupObj = CreateUIObject(name, parent);

            SetLayoutGroup<HorizontalLayoutGroup>(groupObj, forceExpandWidth, forceExpandHeight, spacing, (int)padding.x,
                (int)padding.y, (int)padding.z, (int)padding.w, childAlignment);

            Image image = groupObj.AddComponent<Image>();
            image.color = bgColor == default
                            ? new Color(0.17f, 0.17f, 0.17f)
                            : bgColor;

            return groupObj;
        }

        /// <summary>
        /// Create a GridLayoutGroup object with an Image component. 
        /// </summary>
        public static GameObject CreateGridGroup(GameObject parent, string name, Vector2 cellSize, Vector2 spacing, Color bgColor = default)
        {
            GameObject groupObj = CreateUIObject(name, parent);

            GridLayoutGroup gridGroup = groupObj.AddComponent<GridLayoutGroup>();
            gridGroup.childAlignment = TextAnchor.UpperLeft;
            gridGroup.cellSize = cellSize;
            gridGroup.spacing = spacing;

            Image image = groupObj.AddComponent<Image>();

            image.color = bgColor == default
                ? new Color(0.17f, 0.17f, 0.17f)
                : bgColor;

            return groupObj;
        }

        #endregion

        #region Control and Graphic Components

        /// <summary>
        /// Create a Text component.
        /// </summary>
        /// <param name="parent">The parent object to build onto</param>
        /// <param name="name">The GameObject name of your label</param>
        /// <param name="defaultText">The default text of the label</param>
        /// <param name="alignment">The alignment of the Text component</param>
        /// <param name="color">The Text color (default is White)</param>
        /// <param name="supportRichText">Should the Text support rich text? (Can be changed afterwards)</param>
        /// <param name="fontSize">The default font size</param>
        /// <returns>Your new Text component</returns>
        public static Text CreateLabel(GameObject parent, string name, string defaultText, TextAnchor alignment = TextAnchor.MiddleLeft,
            Color color = default, bool supportRichText = true, int fontSize = 14)
        {
            GameObject obj = CreateUIObject(name, parent, new(0.8f, 1f));
            Text textComp = obj.AddComponent<Text>();

            SetDefaultTextValues(textComp);

            textComp.text = defaultText;
            textComp.color = color == default ? defaultTextColor : color;
            textComp.supportRichText = supportRichText;
            textComp.alignment = alignment;
            textComp.fontSize = fontSize;
            textComp.resizeTextForBestFit = false;

            return textComp;
        }

        /// <summary>
        /// Create a ButtonRef wrapper and a Button component.
        /// </summary>
        /// <param name="parent">The parent object to build onto</param>
        /// <param name="name">The GameObject name of your button</param>
        /// <param name="text">The default button text</param>
        /// <param name="colors">The ColorBlock used for your Button component</param>
        /// <returns>A ButtonRef wrapper for your Button component.</returns>
        public static GameObject CreateButton(GameObject parent, string name, string text, ColorBlock colors)
        {
            GameObject buttonObj = CreateUIObject(name, parent, smallElementSize);

            GameObject textObj = CreateUIObject("Text", buttonObj);

            Image image = buttonObj.AddComponent<Image>();
            image.type = Image.Type.Sliced;
            image.color = new Color(1, 1, 1, 1);

            Button button = buttonObj.AddComponent<Button>();
            SetDefaultSelectableValues(button);

            colors.colorMultiplier = 1;

            Text textComp = textObj.AddComponent<Text>();
            textComp.text = text;
            SetDefaultTextValues(textComp);
            textComp.alignment = TextAnchor.MiddleCenter;

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            SetButtonDeselectListener(button);

            return button.gameObject;
        }

        // Automatically deselect buttons when clicked.
        public static void SetButtonDeselectListener(Button button)
        {
            button.onClick.AddListener(new Action(() =>
            {
                button.OnDeselect(null);
            }));
        }

        /// <summary>
        /// Create a Slider control component.
        /// </summary>
        /// <param name="parent">The parent object to build onto</param>
        /// <param name="name">The GameObject name of your slider</param>
        /// <param name="slider">Returns the created Slider component</param>
        /// <returns>The root GameObject for your Slider</returns>
        public static GameObject CreateSlider(GameObject parent, string name, out Slider slider)
        {
            GameObject sliderObj = CreateUIObject(name, parent, smallElementSize);

            GameObject bgObj = CreateUIObject("Background", sliderObj);
            GameObject fillAreaObj = CreateUIObject("Fill Area", sliderObj);
            GameObject fillObj = CreateUIObject("Fill", fillAreaObj);
            GameObject handleSlideAreaObj = CreateUIObject("Handle Slide Area", sliderObj);
            GameObject handleObj = CreateUIObject("Handle", handleSlideAreaObj);

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(0.15f, 0.15f, 0.15f, 1.0f);

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.25f);
            bgRect.anchorMax = new Vector2(1f, 0.75f);
            bgRect.sizeDelta = new Vector2(0f, 0f);

            RectTransform fillAreaRect = fillAreaObj.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.anchoredPosition = new Vector2(-5f, 0f);
            fillAreaRect.sizeDelta = new Vector2(-20f, 0f);

            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.type = Image.Type.Sliced;
            fillImage.color = new Color(0.3f, 0.3f, 0.3f, 1.0f);

            fillObj.GetComponent<RectTransform>().sizeDelta = new Vector2(10f, 0f);

            RectTransform handleSlideRect = handleSlideAreaObj.GetComponent<RectTransform>();
            handleSlideRect.sizeDelta = new Vector2(-20f, 0f);
            handleSlideRect.anchorMin = new Vector2(0f, 0f);
            handleSlideRect.anchorMax = new Vector2(1f, 1f);

            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);

            handleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(20f, 0f);

            slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fillObj.GetComponent<RectTransform>();
            slider.handleRect = handleObj.GetComponent<RectTransform>();
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;

            return sliderObj;
        }

        /// <summary>
        /// Create a standard Unity Scrollbar component.
        /// </summary>
        /// <param name="parent">The parent object to build onto</param>
        /// <param name="name">The GameObject name of your scrollbar</param>
        /// <param name="scrollbar">Returns the created Scrollbar component</param>
        /// <returns>The root GameObject for your Scrollbar</returns>
        public static GameObject CreateScrollbar(GameObject parent, string name, out Scrollbar scrollbar)
        {
            GameObject scrollObj = CreateUIObject(name, parent, smallElementSize);

            GameObject slideAreaObj = CreateUIObject("Sliding Area", scrollObj);
            GameObject handleObj = CreateUIObject("Handle", slideAreaObj);

            Image scrollImage = scrollObj.AddComponent<Image>();
            scrollImage.type = Image.Type.Sliced;
            scrollImage.color = new Color(0.1f, 0.1f, 0.1f);

            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.type = Image.Type.Sliced;
            handleImage.color = new Color(0.4f, 0.4f, 0.4f);

            RectTransform slideAreaRect = slideAreaObj.GetComponent<RectTransform>();
            slideAreaRect.sizeDelta = new Vector2(-20f, -20f);
            slideAreaRect.anchorMin = Vector2.zero;
            slideAreaRect.anchorMax = Vector2.one;

            RectTransform handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20f, 20f);

            scrollbar = scrollObj.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;

            SetDefaultSelectableValues(scrollbar);

            return scrollObj;
        }

        /// <summary>
        /// Create a Toggle control component.
        /// </summary>
        /// <param name="parent">The parent object to build onto</param>
        /// <param name="name">The GameObject name of your toggle</param>
        /// <param name="toggle">Returns the created Toggle component</param>
        /// <param name="text">Returns the Text component for your Toggle</param>
        /// <param name="bgColor">The background color of the checkbox</param>
        /// <param name="checkWidth">The width of your checkbox</param>
        /// <param name="checkHeight">The height of your checkbox</param>
        /// <returns>The root GameObject for your Toggle control</returns>
        public static GameObject CreateToggle(GameObject parent, string name, out Toggle toggle, out Text text, Color bgColor = default,
            int checkWidth = 20, int checkHeight = 20)
        {
            // Main obj
            GameObject toggleObj = CreateUIObject(name, parent, smallElementSize);
            SetLayoutGroup<HorizontalLayoutGroup>(toggleObj, false, false, 5, 0, 0, 0, 0, childAlignment: TextAnchor.MiddleLeft);
            toggle = toggleObj.AddComponent<Toggle>();
            toggle.isOn = true;
            SetDefaultSelectableValues(toggle);
            // need a second reference so we can use it inside the lambda, since 'toggle' is an out var.
            Toggle t2 = toggle;
            toggle.onValueChanged.AddListener(new Action<bool>((_) => { t2.OnDeselect(null); }));

            // Check mark background

            GameObject checkBgObj = CreateUIObject("Background", toggleObj);
            Image bgImage = checkBgObj.AddComponent<Image>();
            bgImage.color = bgColor == default ? new Color(0.04f, 0.04f, 0.04f, 0.75f) : bgColor;

            SetLayoutGroup<HorizontalLayoutGroup>(checkBgObj, true, true, 0, 2, 2, 2, 2);
            SetLayoutElement(checkBgObj, minWidth: checkWidth, flexibleWidth: 0, minHeight: checkHeight, flexibleHeight: 0);

            // Check mark image

            GameObject checkMarkObj = CreateUIObject("Checkmark", checkBgObj);
            Image checkImage = checkMarkObj.AddComponent<Image>();
            checkImage.color = new Color(0.8f, 1, 0.8f, 0.3f);

            // Label 

            GameObject labelObj = CreateUIObject("Label", toggleObj);
            text = labelObj.AddComponent<Text>();
            text.text = name;
            text.alignment = TextAnchor.MiddleLeft;
            SetDefaultTextValues(text);

            SetLayoutElement(labelObj, minWidth: 0, flexibleWidth: 0, minHeight: checkHeight, flexibleHeight: 0);

            // References

            toggle.graphic = checkImage;
            toggle.targetGraphic = bgImage;

            return toggleObj;
        }

        /// <summary>
        /// Create a standard InputField control and an InputFieldRef wrapper for it.
        /// </summary>
        /// <param name="parent">The parent object to build onto</param>
        /// <param name="name">The GameObject name of your InputField</param>
        /// <param name="placeHolderText">The placeholder text for your InputField component</param>
        /// <returns>An InputFieldRef wrapper for your InputField</returns>
        public static GameObject CreateInputField(GameObject parent, string name, string placeHolderText)
        {
            GameObject mainObj = CreateUIObject(name, parent);

            Image mainImage = mainObj.AddComponent<Image>();
            mainImage.type = Image.Type.Sliced;
            mainImage.color = new Color(0, 0, 0, 0.5f);

            InputField inputField = mainObj.AddComponent<InputField>();
            Navigation nav = inputField.navigation;
            nav.mode = Navigation.Mode.None;
            inputField.navigation = nav;
            inputField.lineType = InputField.LineType.SingleLine;
            inputField.interactable = true;
            inputField.transition = Selectable.Transition.ColorTint;
            inputField.targetGraphic = mainImage;

            GameObject textArea = CreateUIObject("TextArea", mainObj);
            textArea.AddComponent<RectMask2D>();

            RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = Vector2.zero;
            textAreaRect.offsetMax = Vector2.zero;

            GameObject placeHolderObj = CreateUIObject("Placeholder", textArea);
            Text placeholderText = placeHolderObj.AddComponent<Text>();
            SetDefaultTextValues(placeholderText);
            placeholderText.text = placeHolderText ?? "...";
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            placeholderText.horizontalOverflow = HorizontalWrapMode.Wrap;
            placeholderText.alignment = TextAnchor.MiddleLeft;
            placeholderText.fontSize = 14;

            RectTransform placeHolderRect = placeHolderObj.GetComponent<RectTransform>();
            placeHolderRect.anchorMin = Vector2.zero;
            placeHolderRect.anchorMax = Vector2.one;
            placeHolderRect.offsetMin = Vector2.zero;
            placeHolderRect.offsetMax = Vector2.zero;

            inputField.placeholder = placeholderText;

            GameObject inputTextObj = CreateUIObject("Text", textArea);
            Text inputText = inputTextObj.AddComponent<Text>();
            SetDefaultTextValues(inputText);
            inputText.text = "";
            inputText.color = new Color(1f, 1f, 1f, 1f);
            inputText.horizontalOverflow = HorizontalWrapMode.Wrap;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.fontSize = 14;

            RectTransform inputTextRect = inputTextObj.GetComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = Vector2.zero;
            inputTextRect.offsetMax = Vector2.zero;

            inputField.textComponent = inputText;
            inputField.characterLimit = 16000;

            return inputField.gameObject;
        }

        /// <summary>
        /// Create a standard DropDown control.
        /// </summary>
        /// <param name="parent">The parent object to build onto</param>
        /// <param name="name">The GameObject name of your Dropdown</param>
        /// <param name="dropdown">Returns your created Dropdown component</param>
        /// <param name="defaultItemText">The default displayed text (suggested is 14)</param>
        /// <param name="itemFontSize">The font size of the displayed text</param>
        /// <param name="onValueChanged">Invoked when your Dropdown value is changed</param>
        /// <param name="defaultOptions">Optional default options for the dropdown</param>
        /// <returns>The root GameObject for your Dropdown control</returns>
        public static GameObject CreateDropdown(GameObject parent, string name, out Dropdown dropdown, string defaultItemText, int itemFontSize,
            Action<int> onValueChanged, string[]? defaultOptions = null)
        {
            GameObject dropdownObj = CreateUIObject(name, parent, largeElementSize);

            GameObject labelObj = CreateUIObject("Label", dropdownObj);
            GameObject arrowObj = CreateUIObject("Arrow", dropdownObj);
            GameObject templateObj = CreateUIObject("Template", dropdownObj);
            GameObject viewportObj = CreateUIObject("Viewport", templateObj);
            GameObject contentObj = CreateUIObject("Content", viewportObj);
            GameObject itemObj = CreateUIObject("Item", contentObj);
            GameObject itemBgObj = CreateUIObject("Item Background", itemObj);
            GameObject itemCheckObj = CreateUIObject("Item Checkmark", itemObj);
            GameObject itemLabelObj = CreateUIObject("Item Label", itemObj);

            GameObject scrollbarObj = CreateScrollbar(templateObj, "DropdownScroll", out Scrollbar scrollbar);
            scrollbar.SetDirection(Scrollbar.Direction.BottomToTop, true);

            RectTransform scrollRectTransform = scrollbarObj.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.right;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.pivot = Vector2.one;
            scrollRectTransform.sizeDelta = new Vector2(scrollRectTransform.sizeDelta.x, 0f);

            Text itemLabelText = itemLabelObj.AddComponent<Text>();
            SetDefaultTextValues(itemLabelText);
            itemLabelText.alignment = TextAnchor.MiddleLeft;
            itemLabelText.text = defaultItemText;
            itemLabelText.fontSize = itemFontSize;

            Text arrowText = arrowObj.AddComponent<Text>();
            SetDefaultTextValues(arrowText);
            arrowText.text = "▼";
            RectTransform arrowRect = arrowObj.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1f, 0.5f);
            arrowRect.anchorMax = new Vector2(1f, 0.5f);
            arrowRect.sizeDelta = new Vector2(20f, 20f);
            arrowRect.anchoredPosition = new Vector2(-15f, 0f);

            Image itemBgImage = itemBgObj.AddComponent<Image>();
            itemBgImage.color = new Color(0.25f, 0.35f, 0.25f, 1.0f);

            Toggle itemToggle = itemObj.AddComponent<Toggle>();
            itemToggle.targetGraphic = itemBgImage;
            itemToggle.isOn = true;

            itemToggle.onValueChanged.AddListener(new Action<bool>((val) => { itemToggle.OnDeselect(null); }));
            Image templateImage = templateObj.AddComponent<Image>();
            templateImage.type = Image.Type.Sliced;
            templateImage.color = Color.black;

            ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();
            scrollRect.scrollSensitivity = 35;
            scrollRect.content = contentObj.GetComponent<RectTransform>();
            scrollRect.viewport = viewportObj.GetComponent<RectTransform>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = -3f;

            viewportObj.AddComponent<Mask>().showMaskGraphic = false;

            Image viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.type = Image.Type.Sliced;

            Text labelText = labelObj.AddComponent<Text>();
            SetDefaultTextValues(labelText);
            labelText.alignment = TextAnchor.MiddleLeft;

            Image dropdownImage = dropdownObj.AddComponent<Image>();
            dropdownImage.color = new Color(0.04f, 0.04f, 0.04f, 0.75f);
            dropdownImage.type = Image.Type.Sliced;

            dropdown = dropdownObj.AddComponent<Dropdown>();
            dropdown.targetGraphic = dropdownImage;
            dropdown.template = templateObj.GetComponent<RectTransform>();
            dropdown.captionText = labelText;
            dropdown.itemText = itemLabelText;
            //itemLabelText.text = "DEFAULT";

            dropdown.RefreshShownValue();

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 2f);
            labelRect.offsetMax = new Vector2(-28f, -2f);

            RectTransform templateRect = templateObj.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = new Vector2(0f, 2f);
            templateRect.sizeDelta = new Vector2(0f, 150f);

            RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 0f);
            viewportRect.anchorMax = new Vector2(1f, 1f);
            viewportRect.sizeDelta = new Vector2(-18f, 0f);
            viewportRect.pivot = new Vector2(0f, 1f);

            RectTransform contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = new Vector2(0f, 0f);
            contentRect.sizeDelta = new Vector2(0f, 28f);

            RectTransform itemRect = itemObj.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.sizeDelta = new Vector2(0f, 25f);

            RectTransform itemBgRect = itemBgObj.GetComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;
            itemBgRect.sizeDelta = Vector2.zero;

            RectTransform itemLabelRect = itemLabelObj.GetComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(20f, 1f);
            itemLabelRect.offsetMax = new Vector2(-10f, -2f);
            templateObj.SetActive(false);

            if (onValueChanged != null)
                dropdown.onValueChanged.AddListener(onValueChanged);

            if (defaultOptions != null)
            {
                foreach (string option in defaultOptions)
                    dropdown.options.Add(new Dropdown.OptionData(option));
            }

            return dropdownObj;
        }

        #endregion
    }
}