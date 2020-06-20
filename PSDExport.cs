// Desc: psd转ui
// Author: ollve
// Date: 2020-06-19

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;


namespace Editor.PSD2UI
{
    public class PSDExport
    {
        private static string spriteAtlasPath;
        private static string bigSpritePath;
        private static string XMLPath;

        private static string comPath = "Images/CommonImgs/LittleCommon/LittleCommon";

        private static PSDUI psdUI;
        private static string mSceneSave;
        private static Dictionary<string, Sprite> spriteList;

        private static Font defaultFont = new Font("Arial");
        private static Font toFont = new Font("YanTxb");
        private static GameObject objBase;
        /// <summary>
        /// psd2ugui转换入口
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static void Convert()
        {
            CopyJSXToPS();
            SelectPngFile();
            SelectBigPngFile();
            CollectSprites();
            SelectPSDXmlFile();
        }

        public static void CollectSprites()
        {
            spriteList = new Dictionary<string, Sprite>();

            Object[] ImgAtlas = Resources.LoadAll(spriteAtlasPath, typeof(Sprite));

            for (int j = 0; j < ImgAtlas.Length; j++)
            {
                if (ImgAtlas.Length > 1)
                {
                    spriteList.Add(ImgAtlas[j].name, ImgAtlas[j] as Sprite);
                }
            }

            Object[] imags = Resources.LoadAll(bigSpritePath, typeof(Sprite));
            for (int k = 0; k < imags.Length; k++)
            {
                if (imags.Length > 1)
                {
                    spriteList.Add(imags[k].name, imags[k] as Sprite);
                }
            }


            Object[] comImg = Resources.LoadAll(comPath, typeof(Sprite));
            for (int k = 0; k < comImg.Length; k++)
            {
                if (comImg.Length > 1)
                {
                    spriteList.Add(comImg[k].name, comImg[k] as Sprite);
                }
            }

        }
        /// <summary>
        /// 把psd导出散图和层级信息的脚本拷贝到每个人的psd安装目录
        /// </summary>
        [MenuItem("Tools/Copy JSX To PS (复制jsx脚本到PS安装目录)", false, 100)]
        private static void CopyJSXToPS()
        {
            var psdScriptFullPath = Path.Combine(LocalPathConfigParser.Instance.PsInstallPath,  "Presets/Scripts/Export PSDUI.jsx");
            var fileInfo = new FileInfo(psdScriptFullPath);
            File.Copy(Path.Combine(Path.GetFullPath("."), "Assets/Editor/PSD2UI/Export PSDUI.jsx"),  psdScriptFullPath, true); 
        }

        /// <summary>
        /// 选择psd需要的图集资源路径
        /// </summary>
        /// <exception cref="Exception"></exception>
        private static void SelectPngFile()
        {
            string inputFile = EditorUtility.OpenFilePanel("Choose PSDUI Sprite Assets to Import", Application.dataPath + "/Resources/Images/", "png");
            if (string.IsNullOrEmpty(inputFile))
            {
                throw new Exception("请选择对应的png文件");
            }
            
            if (!inputFile.StartsWith(Application.dataPath+"/Resources"))
            {
                throw new Exception("请确保png文件已经存在于Unity项目的Resources目录之下！！！");
            }

            string tempPath = inputFile.Remove(0, inputFile.LastIndexOf("Resources") + 10);//Resources下的资源路径
            string pngPath = tempPath.Remove(tempPath.LastIndexOf(".png"),4);//去除格式
            spriteAtlasPath = pngPath;
        }

        /// <summary>
        /// 选择psd需要的大图资源路径
        /// </summary>
        private static void SelectBigPngFile()
        {
            string inputFile = EditorUtility.OpenFolderPanel("Choose PSDUI Folder Assets to Import", Application.dataPath + "/Resources/Images/", "");
            if (string.IsNullOrEmpty(inputFile))
            {
                throw new Exception("请选择对应的文件夹");
            }

            if (!inputFile.StartsWith(Application.dataPath + "/Resources"))
            {
                throw new Exception("请确保文件夹已经存在于Unity项目的Resources目录之下！！！");
            }
            string tempPath = inputFile.Remove(0, inputFile.LastIndexOf("Resources") + 10);//Resources下的资源路径

            bigSpritePath = tempPath;//大图资源路径
        }
        /// <summary>
        /// 选择psd里面导出的xml信息
        /// </summary>
        /// <exception cref="Exception"></exception>
        private static void SelectPSDXmlFile()
        {
            string inputFile = EditorUtility.OpenFilePanel("Choose PSDUI XMLFile to Import", Application.dataPath+ "/Resources/Layout/", "xml");

            XMLPath = inputFile.Remove(0, inputFile.LastIndexOf("Assets"));
            if (string.IsNullOrEmpty(inputFile))
            {
                throw new Exception("请选择对应的xml文件");
            }

            if (!inputFile.StartsWith(Application.dataPath))
            {
                throw new Exception("请确保xml文件和对应的psd散图已经存在于Unity项目的Assets目录之下！！！");
            }

            ClearScene();

            ImportPSDUI(inputFile);
            //重新加载Game主场景

            RestoreScene();
        }

        private static void ClearScene()
        {
            mSceneSave = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
        }

        private static void RestoreScene()
        {
            if (!string.IsNullOrEmpty(mSceneSave))
                EditorSceneManager.OpenScene(mSceneSave);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xmlFilePath">jsx导出的xml文件</param>
        static private void ImportPSDUI(string xmlFilePath)
        {
            psdUI = (PSDUI) DeserializeXml(xmlFilePath, typeof(PSDUI));

            Debug.LogFormat("=====psdSize======{0}:{1}", psdUI.psdSize.width, psdUI.psdSize.height);
           
            if (psdUI == null)
            {
                Debug.Log("The file " + xmlFilePath + " wasn't able to generate a PSDUI.");
                return;
            }

            string baseFilename = Path.GetFileNameWithoutExtension(xmlFilePath);
            string baseDirectory = String.Format("Assets/{0}/", Path.GetDirectoryName(xmlFilePath.Remove(0, Application.dataPath.Length + 1)));

            objBase = new GameObject(baseFilename);
            objBase.AddComponent<RectTransform>().sizeDelta = new Vector2(psdUI.psdSize.width, psdUI.psdSize.height);

            for (int i = 0; i < psdUI.layers.Length; i++)
            {
                DrawLayer(psdUI.layers[i]);
            }

            PrefabUtility.CreatePrefab(baseDirectory + baseFilename + ".prefab", objBase);
            //删除XML文件
            if (File.Exists(XMLPath))
            {
                File.Delete(XMLPath);
            }

            AssetDatabase.Refresh();
        }


        /// <summary>
        /// 画各种不同的层显示对象
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="parent"></param>
        private static void DrawLayer(PSDUI.Layer layer )
        {
            switch (layer.type)
            {
                case PSDUI.LayerType.Normal:
                    DrawNormalLayer(layer);
                    break;
                case PSDUI.LayerType.Button:
                    DrawButtonLayer(layer);
                    break;
                case PSDUI.LayerType.Label:
                    DrawLableLayer(layer);
                    break;
                case PSDUI.LayerType.Text:
                    DrawTextLayer(layer);
                    break;
                case PSDUI.LayerType.Image:
                    DrawImageLayer(layer);
                    break;
            }
        }

        private static void DrawImageLayer(PSDUI.Layer layer )
        {
            GameObject obj = new GameObject(layer.name);
            if (layer.parent != string.Empty)
            {
                obj.transform.parent = objBase.transform.Find(layer.parent);
            }
            else
            {
                obj.transform.parent = objBase.transform;
            }
            DrawSprite(layer, obj);
            obj.GetComponent<Image>().raycastTarget = false;
        }

        private static void DrawTextLayer(PSDUI.Layer layer )
        {
            IsDefaultFont(layer,  false);
        }

        private static void DrawLableLayer(PSDUI.Layer layer )
        {
            IsDefaultFont(layer,  true);
        }

        private static void IsDefaultFont(PSDUI.Layer layer,  bool isDefault)
        {
            GameObject obj = new GameObject(layer.name);
            if (layer.parent != string.Empty)
            {
                obj.transform.parent = objBase.transform.Find(layer.parent);
            }
            else
            {
                obj.transform.parent = objBase.transform;
            }

            obj.name = layer.name;
            obj.AddComponent<RectTransform>().sizeDelta = new Vector2(layer.size.width, layer.size.height);
            obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(layer.position.x, layer.position.y);
            Text objText = obj.AddComponent<Text>();
            objText.text = layer.name;
            objText.raycastTarget = false;

            int b = 20;
            string[] strList = layer.arguments.size.Split('.');
            bool rlt = int.TryParse(strList[0], out b);
            objText.fontSize = b;
            Color color;
            ColorUtility.TryParseHtmlString("#" + layer.arguments.color, out color);
            objText.color = color;
            if(isDefault)
                objText.font = defaultFont;
            else
                objText.font = toFont;
        }

        private static void DrawButtonLayer(PSDUI.Layer layer )
        {
            GameObject obj = new GameObject(layer.name);
            if (layer.parent != string.Empty)
            {
                obj.transform.parent = objBase.transform.Find(layer.parent);
            }
            else
            {
                obj.transform.parent = objBase.transform;
            }
            DrawSprite(layer, obj);
            obj.GetComponent<Image>().raycastTarget = true;
            obj.AddComponent<Button>();
        }

        private static void DrawSprite(PSDUI.Layer layer, GameObject obj)
        {
            obj.name = layer.name;
            obj.AddComponent<RectTransform>().sizeDelta = new Vector2(layer.size.width, layer.size.height);
            obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(layer.position.x, layer.position.y);
            obj.AddComponent<Image>().sprite = GetSprite(layer.name);
        }


        private static void DrawNormalLayer(PSDUI.Layer layer )
        {
            GameObject obj = new GameObject(layer.name);
            if (layer.parent != string.Empty)
            {
                obj.transform.parent = objBase.transform.Find(layer.parent);
            }
            else
            { 
                obj.transform.parent = objBase.transform;
            }

            obj.name = layer.name;
            obj.AddComponent<RectTransform>().sizeDelta = new Vector2(psdUI.psdSize.width, psdUI.psdSize.height);
        }

        /// <summary>
        /// 获取图片
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static Sprite GetSprite(string name)
        {
            if (spriteList.ContainsKey(name))
                return spriteList[name];
            else
                return null;
        }

        /// <summary>
        /// 解析jsx导出的布局文件
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private  static object DeserializeXml(string filePath, Type type)
        {
            object instance = null;
            StreamReader xmlFile = File.OpenText(filePath);
            if (xmlFile != null)
            {
                string xml = xmlFile.ReadToEnd();
                if ((xml != null) && (xml.ToString() != ""))
                {
                    XmlSerializer xs = new XmlSerializer(type);
                    UTF8Encoding encoding = new UTF8Encoding();
                    byte[] byteArray = encoding.GetBytes(xml);
                    MemoryStream memoryStream = new MemoryStream(byteArray);
                    XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);
                    if (xmlTextWriter != null)
                    {
                        instance = xs.Deserialize(memoryStream);
                    }
                }
            }

            xmlFile.Close();
            return instance;
        }

    }
   
}