using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DockingCamera
{
    public class BaseKspCamera
    {


        protected static int windowCount = 0;
        protected static double ElectricChargeAmount;
        public static Material CurrentShader;
        public static string CurrentShaderName;

        protected Rect windowPosition;
        protected Rect texturePosition;
        
        protected string windowLabel;
        protected string subWindowLabel; 
        protected GameObject partGameObject;
        protected Part part;
        protected Texture textureBackGroundCamera;
        protected Texture textureSeparator;
        protected Texture textureTargetMark;
        protected Texture[] textureNoSignal;
        protected RenderTexture renderTexture; 
        protected ShaderType shaderType;

        protected float windowSize = 128f; 
        protected float rotateX = 0;
        protected float rotateY = 0;
        protected float rotateZ = 0;
        protected float minZoom = 1f;
        internal float maxZoom = 32f;
        internal float currentZoom = 32f;
        protected float minZoomMultiplier = 4;

        protected bool ShaderSwitcher = false;
        protected bool zoomMultiplier = false; 
        protected bool zoomWide = false;
        protected bool isTargetPoint = false;

        protected int windowSizeCoef = 2;
        protected int windowId;
        protected int textureNoSignalId;


        public bool IsActivate = false;
        public bool IsAuxiliaryWindowOpen = false;
        public bool IsAuxiliaryWindowButtonPres = false;
        public bool IsButtonOff = false;
        public bool IsOrbital = false;

        protected List<Camera> allCameras = new List<Camera>();
        protected List<GameObject> allCamerasGameObject = new List<GameObject>();
        protected List<string> cameraNames = new List<string>{"GalaxyCamera", "Camera ScaledSpace", "Camera 01", "Camera 00" };

		//UPDATE_MARK
		protected UpdateGUIObject updateGUIObject;
        
        public BaseKspCamera(Part part, int windowSize, string windowLabel = "Camera")
        {
            this.windowSize = windowSize/2;
            this.part = part;
            subWindowLabel = windowLabel;
            this.windowLabel = windowLabel;
            partGameObject = part.gameObject;
            InitWindow();
            InitTextures();
            //windowCount++;
            GameEvents.OnFlightUIModeChanged.Add(FlightUIModeChanged);
			//UPDATE_MARK
			GameObject updateGUIHolder = new GameObject();
 			updateGUIObject = updateGUIHolder.AddComponent<UpdateGUIObject>(); // добавление компонента на объект
			updateGUIHolder.transform.parent = part.transform;
            //updateGUIObject.awakeFunction += Awake; //добавление скриптов monobehaviour к выполнению тут
        }

        ~BaseKspCamera()
        {
            GameEvents.OnFlightUIModeChanged.Remove(FlightUIModeChanged);
        }

        private void FlightUIModeChanged(FlightUIMode mode)
        {
            if (mode == FlightUIMode.ORBITAL)
            {
                IsOrbital = true;
            }
            else
            {
                IsOrbital = false;
            }
        }
        
        /// <summary>
        /// Initializes window
        /// </summary>
        protected virtual void InitWindow()
        {
            windowId = UnityEngine.Random.Range(1000, 10000);
            if (windowPosition.yMin < 64)
                windowPosition.yMin = 64;
            windowPosition.width = windowSize * windowSizeCoef;
            windowPosition.height = windowSize * windowSizeCoef + 34f; //38f;
        }

        /// <summary>
        /// Initializes texture
        /// </summary>
        protected virtual void InitTextures()
        {
            texturePosition = new Rect(6, 34, windowPosition.width - 12f, windowPosition.height - 40f); //42f);
            renderTexture = new RenderTexture((int)windowSize * 4, (int)windowSize * 4, 24, RenderTextureFormat.RGB565);  
            RenderTexture.active = renderTexture;
            renderTexture.Create();
            textureBackGroundCamera = Util.MonoColorRectTexture(new Color(0.45f, 0.45f, 0.45f, 1));
            textureSeparator = Util.MonoColorVerticalLineTexture(Color.white, (int)texturePosition.height);
            textureTargetMark = AssetLoader.texTargetPoint;
            textureNoSignal = new Texture[8];
            for (int i = 0; i < textureNoSignal.Length; i++)
            {
                textureNoSignal[i] = Util.WhiteNoiseTexture((int) texturePosition.width, (int) texturePosition.height, 1f); 
                //Util.LoadTexture("nosignal");
            }
        }

        /// <summary>
        /// Initializes camera
        /// </summary>
        protected virtual void InitCameras()
        {
            allCamerasGameObject = cameraNames.Select(a => new GameObject()).ToList();
            allCameras = allCamerasGameObject.Select((go, i) =>
                {
                    var camera = go.AddComponent<Camera>();
                    var cameraExample = Camera.allCameras.FirstOrDefault(cam => cam.name == cameraNames[i]);
                    if (cameraExample != null)
                    {
                        camera.CopyFrom(cameraExample);
                        camera.name = string.Format("{1} copy of {0}", cameraNames[i], windowCount);
                        camera.targetTexture = renderTexture;
                    }
                    return camera;
                }).ToList();
        }

        /// <summary>
        /// Destroy Cameras
        /// </summary>
        protected virtual void DestroyCameras()
        {
            allCameras.ForEach(Camera.Destroy);
            allCameras = new List<Camera>();
        }

        /// <summary>
        /// Create and activate cameras
        /// </summary>
        public virtual void Activate()
        {
            if (IsActivate) return;
            windowCount++;
            InitCameras();
            IsActivate = true;
            updateGUIObject.updateGUIFunction += Begin;
        }

        /// <summary>
        /// Destroy  cameras
        /// </summary>
        public virtual void Deactivate()
        {
            if (!IsActivate) return;
            windowCount--;
            DestroyCameras();
            IsActivate = false;
            updateGUIObject.updateGUIFunction -= Begin;
        }

        void Begin()
        {
			if (IsActivate)
			{
                windowPosition = GUI.Window(windowId, windowPosition, DrawWindow, windowLabel); //draw main window

                ElectricChargeAmount = part.vessel.GetActiveResources().First(x => x.info.name == "ElectricCharge").amount;
                if (ElectricChargeAmount <= 0)
                {
                    ScreenMessages.PostScreenMessage("NOT ENOUGH ENERGY", 3f, ScreenMessageStyle.UPPER_CENTER);
                    IsButtonOff = true;
                }
                if (HighLogic.LoadedSceneIsFlight && !FlightDriver.Pause)
                    part.RequestResource("ElectricCharge", 0.002 * TimeWarp.fixedDeltaTime);               
			}
		}

#region DRAW LAYERS 

        /// <summary>
        /// drawing method
        /// </summary>
        private void DrawWindow(int id)
        {
            ExtendedDrawWindowL1();
            ExtendedDrawWindowL2();
            ExtendedDrawWindowL3();
            GUI.DragWindow();
        }

        /// <summary>
        /// drawing method, first layer, for cameras
        /// </summary>
        protected virtual void ExtendedDrawWindowL1()
        {
            var widthOffset = windowPosition.width - 86;
            if (!zoomMultiplier)
                GUI.Label(new Rect(widthOffset, 140, 77, 20), "zoom: " + (int)(maxZoom - currentZoom + minZoom));
            else
                GUI.Label(new Rect(widthOffset, 140, 77, 20), "zoom: " + (int)(maxZoom - currentZoom + minZoom) * 24);

            isTargetPoint = GUI.Toggle(new Rect(widthOffset, 230, 77, 40), isTargetPoint, "Target\nMark");

            Graphics.DrawTexture(texturePosition, textureBackGroundCamera);
            CurrentShader = CameraShaders.Get(shaderType);
            if (CurrentShader == null)
            CurrentShaderName = "none";
            else
                CurrentShaderName = CurrentShader.name;
            Graphics.DrawTexture(texturePosition, Render(), CurrentShader);
        }

        /// <summary>
        /// drawing method, second layer (draw any texture between cam and interface)
        /// </summary>
        protected virtual void ExtendedDrawWindowL2()
        {
            if (TargetHelper.IsTargetSelect)
            {
                var camera = allCameras.Last();
                var vessel = TargetHelper.Target as Vessel;

                if (vessel == null)
                {
                    var targetedDockingNode = TargetHelper.Target as ModuleDockingNode;
                    vessel = targetedDockingNode.vessel;
                }

                var point = camera.WorldToViewportPoint(vessel.transform.position); //get current targeted vessel 
                var x = point.x; //(0;1)
                var y = point.y;
                var z = point.z;
 
                x = GetX(x,z);
                y = GetY(y,z);

                var offsetX = texturePosition.width * x;
                var offsetY = texturePosition.height * y;

                if (isTargetPoint)
                {
                    GUI.DrawTexture(new Rect(texturePosition.xMin + offsetX-10, texturePosition.yMax - offsetY-10, 20, 20), textureTargetMark);
                }
            }
            if (IsOrbital)
            {
                Graphics.DrawTexture(texturePosition, textureNoSignal[textureNoSignalId]); 
            }
        }

        private float GetX(float x, float z)
        {
            if (x < 0 && z > 0)
            {
                if (x <= 0) x = 0f;
                //if (x <= -0.02) x = -0.02f;
                return x;
            }
            if (x > 0 && z < 0)
            {
                x = 0f;//-0.02f;
                return x;
            }
            if (x < 0 && z < 0)
            {
                x = 1f;//0.96f;
                return x;
            }
            if (x > 0 && z > 0)
            {
                if (x >= 1) x = 1f;
                //if (x >= 0.96) x = 0.96f;
                return x;
            }
            return x;
        }
        private float GetY(float y, float z)
        {
            if (z > 0)
            {
                if (y <= 0f)//0.02)
                {
                    y = 0f;//0.02f;
                    return y;
                }
                if (y >= 1f) //1.02)
                {
                    y = 1f;//1.02f;
                    return y;
                }
            }
            if (z < 0)
            {
                if (y <= 0)//0.02)
                {
                    y = 0;//0.02f;
                    return y;
                }
                if (y >= 1f)//1.02)
                {
                    y = 1f;//1.02f;
                    return y;
                }
            }
            return y;
        }
        /// <summary>
        /// drawing method, third layer, interface
        /// </summary>
        protected virtual void ExtendedDrawWindowL3()  
        {
            if (!part.vessel.Equals(FlightGlobals.ActiveVessel))
            {
                GUI.Label(new Rect(55, 33, 160, 20), "Translation from: " + part.vessel.vesselName);
            }
            if (IsAuxiliaryWindowOpen)
                GUI.DrawTexture(new Rect(texturePosition.width+8, 34, 1, texturePosition.height), textureSeparator);  //vert line, textureSeparator

            if (GUI.Button(new Rect(windowPosition.width - 20, 3, 15, 15), " ")) // destroy cam window
            {
                IsButtonOff = true;
            } 
            if (GUI.Button(new Rect(windowPosition.width - 29, 18, 24, 15), IsAuxiliaryWindowOpen ? "◄" : "►")) //extend window
            {
                IsAuxiliaryWindowOpen = !IsAuxiliaryWindowOpen;
                IsAuxiliaryWindowButtonPres = true;
            }

            var aaa = new Rect(8, texturePosition.yMax - 22, 20, 20);
            var bbb = new GUIContent("☼", CurrentShaderName);
            GUI.Box(new Rect(aaa), bbb);
            GUI.Label(new Rect(64, 128, 200, 40), GUI.tooltip);
            if (GUI.Button(new Rect(aaa), "☼")) //shader switch "▼" 
            {
                shaderType++;
                if (!Enum.IsDefined(typeof(ShaderType), shaderType))
                    shaderType = ShaderType.OldTV;
            }

            if (GUI.RepeatButton(new Rect(texturePosition.xMax - 22, texturePosition.yMax - 22, 20, 20), "±") && 	
                Camera.allCameras.FirstOrDefault(x => x.name == "Camera 00") != null) //Size of main window
            {
                switch (windowSizeCoef)
                {
                    case 2:
                        windowSizeCoef = 3;
                        break;
                    case 3:
                        windowSizeCoef = 2; 
                        break;
                }
                Deactivate();
                InitWindow();
                InitTextures();
                Activate();
                IsAuxiliaryWindowOpen = false;
            }

            var left = texturePosition.width / 2 - 80;
            currentZoom = GUI.HorizontalSlider(new Rect(left, 20, 160, 10), currentZoom, maxZoom, minZoom);
        }

#endregion DRAW LAYERS

        /// <summary>
        /// render texture camera
        /// </summary>
        protected virtual RenderTexture Render()
        {
            allCameras.ForEach(a => a.Render());
            return renderTexture;
        }

        public IEnumerator ResizeWindow()
        {
            IsAuxiliaryWindowButtonPres = false;
            while (true)
            {
                if (IsAuxiliaryWindowOpen && windowPosition.width < windowSize * windowSizeCoef + 88)
                {
                    windowPosition.width += 4;
                    if (windowPosition.width >= windowSize*windowSizeCoef + 88)
                        break;
                }
                else if (windowPosition.width > windowSize*windowSizeCoef)
                {
                    windowPosition.width -= 4;
                    if (windowPosition.width <= windowSize*windowSizeCoef)
                        break;
                }
                else
                    break;
                yield return new WaitForSeconds(1/20);
            }
        }

        protected void UpdateWhiteNoise()
        {
            if (!IsOrbital) return;
            textureNoSignalId++;
            if (textureNoSignalId >= textureNoSignal.Length)
                textureNoSignalId = 0;
        }
        /// <summary>
        /// Update position and rotation of the cameras
        /// </summary>
        public virtual void Update()
        {
            allCamerasGameObject.Last().transform.position = partGameObject.transform.position;
            allCamerasGameObject.Last().transform.rotation = partGameObject.transform.rotation;

            allCamerasGameObject.Last().transform.Rotate(new Vector3(-1f, 0, 0), 90);
            allCamerasGameObject.Last().transform.Rotate(new Vector3(0, 1f, 0), rotateY);
            allCamerasGameObject.Last().transform.Rotate(new Vector3(1f, 0, 0), rotateX);
            allCamerasGameObject.Last().transform.Rotate(new Vector3(0, 0, 1f), rotateZ);

            allCamerasGameObject[0].transform.rotation = allCamerasGameObject.Last().transform.rotation;
            allCamerasGameObject[1].transform.rotation = allCamerasGameObject.Last().transform.rotation;
            allCamerasGameObject[2].transform.rotation = allCamerasGameObject.Last().transform.rotation;
            allCamerasGameObject[2].transform.position = allCamerasGameObject.Last().transform.position;
            allCameras.ForEach(cam => cam.fieldOfView = currentZoom);
        }
    }
}
