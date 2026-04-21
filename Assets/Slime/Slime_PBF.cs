using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.InputSystem;

namespace Slime
{
    public class Slime_PBF : MonoBehaviour
    {
        [System.Serializable]
        public enum RenderMode
        {
            Particles,
            Surface,
        }
        
        private struct SlimeInstance
        {
            public bool Active;
            public float3 Center;
            public Vector3 Pos;
            public Vector3 Dir;
            public float Radius;
            public int ControllerID;
        }
        

        [SerializeField, Range(0, 1)] private float bubbleSpeed = 0.2f;
        [SerializeField, Range(0, 100)] private float viscosityStrength = 1.0f;
        [SerializeField, Range(0.1f, 100)] private float concentration = 10f;
        public float gravity = -5f;
        [SerializeField, Range(0, 5)] private float threshold = 1f;
        [SerializeField] private bool useAnisotropic = false;
        
        [SerializeField] private Mesh faceMesh;
        [SerializeField] private Material faceMat;
        [SerializeField] private Material mat;
        [SerializeField] private Mesh particleMesh;
        [SerializeField] private Material particleMat;
        [SerializeField] private Material bubblesMat;
        
        [Header("Freezing Effect")]
        public float freezeSpeed = 2.0f; // How fast it transitions
        public Material frozenMat;       // Assign the ice material in the Inspector
        public float frozenViscosity = 100f;

        private float _originalViscosity;
        private float _originalBubbleSpeed;
        private float _freezeFactor = 0f; // 0 = liquid, 1 = frozen
        public bool isFrozen = false; // Toggled by the SlimeFreezer script
        [Header("Fog Toggles")]
        public bool isFog = false; // Toggles visual rendering so particles can seamlessly take over
        public float fogGravity = -0.5f; // Decreased gravity so fog glides softly
        public float fogSphereRadius = 0.1f; // Smaller radius for fog form sphere collider
        public float normalSphereRadius = 0.25f; // Normal radius for slime form sphere collider
        public ParticleSystem fogParticles; // The unity particles attached to the player instance
        private bool _wasFog = false;
        private float _lastFormChangeTime = 0f;

        public Transform trans;

        public void HeatUp()
        {
            if (Time.time - _lastFormChangeTime < 1.0f) return;

            if (isFrozen) 
            {
                isFrozen = false; // Ice -> Slime
                _lastFormChangeTime = Time.time;
            }
            else if (!isFog) 
            {
                isFog = true; // Slime -> Steam
                _lastFormChangeTime = Time.time;
            }
        }

        public void CoolDown()
        {
            if (Time.time - _lastFormChangeTime < 1.0f) return;

            if (isFog) 
            {
                isFog = false; // Steam -> Slime
                _lastFormChangeTime = Time.time;
            }
            else if (!isFrozen) 
            {
                isFrozen = true; // Slime -> Ice
                _lastFormChangeTime = Time.time;
            }
        }
        public RenderMode renderMode = RenderMode.Surface;
        public int blockNum;
        public int bubblesNum;
        public float3 minPos;
        public float3 maxPos;

        public bool gridDebug;
        public bool componentDebug;

        #region Buffers
        
        private NativeArray<Particle> _particles;
        private NativeArray<Particle> _particlesTemp;
        private NativeArray<float3> _posPredict;
        private NativeArray<float3> _posOld;
        private NativeArray<float> _lambdaBuffer;
        private NativeArray<float3> _velocityBuffer;
        private NativeArray<float3> _velocityTempBuffer;
        private NativeHashMap<int, int2> _lut;
        private NativeArray<int2> _hashes;
        private NativeArray<float4x4> _covBuffer;
        private NativeArray<MyBoxCollider> _colliderBuffer;
        
        private NativeArray<float3> _boundsBuffer;
        private NativeArray<float> _gridBuffer;
        private NativeArray<float> _gridTempBuffer;
        private NativeHashMap<int3, int> _gridLut;
        private NativeArray<int4> _blockBuffer;
        private NativeArray<int> _blockColorBuffer;
        private NativeArray<float3> _frozenOffsets;
        private bool _wasFrozen = false;
        
        private NativeArray<Effects.Bubble> _bubblesBuffer;
        private NativeList<int> _bubblesPoolBuffer;
        
        private NativeList<Effects.Component> _componentsBuffer;
        private NativeArray<int> _gridIDBuffer;
        private NativeList<ParticleController> _controllerBuffer;
        private NativeList<ParticleController> _lastControllerBuffer;
        
        private ComputeBuffer _particlePosBuffer;
        private ComputeBuffer _particleCovBuffer;
        private ComputeBuffer _bubblesDataBuffer;
        
        #endregion
        
        private float3 _lastMousePos;
        private bool _mouseDown;
        private float3 _velocityY = float3.zero;
        private Bounds _bounds;
        private Vector3 _velocity = Vector3.zero;

        private LMarchingCubes _marchingCubes;
        private Mesh _mesh;
        
        private int batchCount = 64;
        private bool _connect;
        private NativeList<SlimeInstance> _slimeInstances;
        private int _controlledInstance;
        private Stack<int> _instancePool;

        void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            if (mat != null) mat.enableInstancing = true;
        }

        void Start()
        {
            _particles = new NativeArray<Particle>(PBF_Utils.Num, Allocator.Persistent);
            float half = PBF_Utils.Width / 2.0f;
            for (int i = 0; i < PBF_Utils.Width / 2; i++)
            for (int j = 0; j < PBF_Utils.Width; j++)
            for (int k = 0; k < PBF_Utils.Width; k++)
            {
                var idx = i * PBF_Utils.Width * PBF_Utils.Width + j * PBF_Utils.Width + k;
                _particles[idx] = new Particle
                {
                    Position = new float3(k - half, j, i - half) * 0.5f,
                    ID = 0,
                };
            }

            int particleNum = PBF_Utils.Num;
            _particlesTemp = new NativeArray<Particle>(particleNum, Allocator.Persistent);
            _posPredict = new NativeArray<float3>(particleNum, Allocator.Persistent);
            _posOld = new NativeArray<float3>(particleNum, Allocator.Persistent);
            _lambdaBuffer = new NativeArray<float>(particleNum, Allocator.Persistent);
            _velocityBuffer = new NativeArray<float3>(particleNum, Allocator.Persistent);
            _velocityTempBuffer = new NativeArray<float3>(particleNum, Allocator.Persistent);
            _boundsBuffer = new NativeArray<float3>(2, Allocator.Persistent);
            _gridBuffer = new NativeArray<float>(PBF_Utils.GridSize * PBF_Utils.GridNum, Allocator.Persistent);
            _gridTempBuffer = new NativeArray<float>(PBF_Utils.GridSize * PBF_Utils.GridNum, Allocator.Persistent);
            _gridLut = new NativeHashMap<int3, int>(PBF_Utils.GridNum, Allocator.Persistent);
            _covBuffer = new NativeArray<float4x4>(particleNum, Allocator.Persistent);
            _blockBuffer = new NativeArray<int4>(PBF_Utils.GridNum, Allocator.Persistent);
            _blockColorBuffer = new NativeArray<int>(9, Allocator.Persistent);
            _frozenOffsets = new NativeArray<float3>(particleNum, Allocator.Persistent);
            
            _bubblesBuffer  = new NativeArray<Effects.Bubble>(PBF_Utils.BubblesCount, Allocator.Persistent);
            _bubblesPoolBuffer = new NativeList<int>(PBF_Utils.BubblesCount, Allocator.Persistent);
            for (int i = 0; i < PBF_Utils.BubblesCount; ++i)
            {
                _bubblesBuffer[i] = new Effects.Bubble()
                {
                    LifeTime = -1,
                };
                _bubblesPoolBuffer.Add(i);
            }

            _lut = new NativeHashMap<int, int2>(particleNum, Allocator.Persistent);
            _hashes = new NativeArray<int2>(particleNum, Allocator.Persistent);
            
            _componentsBuffer = new NativeList<Effects.Component>(16, Allocator.Persistent);
            _gridIDBuffer = new NativeArray<int>(PBF_Utils.GridSize * PBF_Utils.GridNum, Allocator.Persistent);
            _controllerBuffer = new NativeList<ParticleController>(16, Allocator.Persistent);
            _controllerBuffer.Add(new ParticleController
            {
                Center = float3.zero,
                Radius = PBF_Utils.InvScale,
                Velocity = float3.zero,
                Concentration = concentration,
            });
            _lastControllerBuffer = new NativeList<ParticleController>(16, Allocator.Persistent);

            _marchingCubes = new LMarchingCubes();

            _particlePosBuffer = new ComputeBuffer(particleNum, sizeof(float) * 4);
            _particleCovBuffer = new ComputeBuffer(particleNum, sizeof(float) * 16);
            _bubblesDataBuffer  = new ComputeBuffer(PBF_Utils.BubblesCount, sizeof(float) * 8);
            particleMat.SetBuffer("_ParticleBuffer", _particlePosBuffer);
            particleMat.SetBuffer("_CovarianceBuffer", _particleCovBuffer);
            bubblesMat.SetBuffer("_BubblesBuffer", _bubblesDataBuffer);

            _slimeInstances = new NativeList<SlimeInstance>(16,  Allocator.Persistent);
            _slimeInstances.Add(new SlimeInstance()
            {
                Center = Vector3.zero,
                Pos = Vector3.zero,
                Dir = Vector3.right,
                Radius = 1
            });
            _instancePool = new Stack<int>();
            var colliders = GetComponentsInChildren<BoxCollider>();
            _colliderBuffer = new NativeArray<MyBoxCollider>(colliders.Length, Allocator.Persistent);
            for (int i = 0; i < colliders.Length; ++i)
            {
                _colliderBuffer[i] = new MyBoxCollider()
                {
                    Center = colliders[i].bounds.center * PBF_Utils.InvScale,
                    Extent = colliders[i].bounds.extents * PBF_Utils.InvScale + Vector3.one,
                };
            }
            _originalViscosity = viscosityStrength;
            _originalBubbleSpeed = bubbleSpeed;

            // Set initial sphere collider radius to public normal radius
            if (TryGetComponent<SphereCollider>(out var sc))
                sc.radius = normalSphereRadius;


            // Anisotropic covariance runs a per-particle EVD — disabled for performance.
            useAnisotropic = false;
        }

        public struct TeleportParticlesJob : IJobParallelFor
        {
            public NativeArray<Particle> Ps;
            public float3 Offset;
            public void Execute(int i)
            {
                Particle p = Ps[i];
                p.Position += Offset;
                Ps[i] = p;
            }
        }

        public struct TeleportPosJob : IJobParallelFor
        {
            public NativeArray<float3> PosList;
            public float3 Offset;
            public void Execute(int i)
            {
                PosList[i] += Offset;
            }
        }

        public void TeleportSystem(Vector3 offset)
        {
            // Translates all raw PBF memory instantly to prevent drag swooshing or physics explosions
            float3 shift = (float3)offset * PBF_Utils.InvScale;

            if (_particles.IsCreated) new TeleportParticlesJob { Ps = _particles, Offset = shift }.Schedule(_particles.Length, batchCount).Complete();
            if (_particlesTemp.IsCreated) new TeleportParticlesJob { Ps = _particlesTemp, Offset = shift }.Schedule(_particlesTemp.Length, batchCount).Complete();
            if (_posPredict.IsCreated) new TeleportPosJob { PosList = _posPredict, Offset = shift }.Schedule(_posPredict.Length, batchCount).Complete();
            if (_posOld.IsCreated) new TeleportPosJob { PosList = _posOld, Offset = shift }.Schedule(_posOld.Length, batchCount).Complete();
        }

        private void OnDestroy()
        {
            if (_particles.IsCreated) _particles.Dispose();
            if (_particlesTemp.IsCreated) _particlesTemp.Dispose();
            if (_lut.IsCreated) _lut.Dispose();
            if (_hashes.IsCreated) _hashes.Dispose();
            if (_posPredict.IsCreated) _posPredict.Dispose();
            if (_posOld.IsCreated) _posOld.Dispose();
            if (_lambdaBuffer.IsCreated) _lambdaBuffer.Dispose();
            if (_velocityBuffer.IsCreated) _velocityBuffer.Dispose();
            if (_velocityTempBuffer.IsCreated) _velocityTempBuffer.Dispose();
            if (_boundsBuffer.IsCreated) _boundsBuffer.Dispose();
            if (_gridBuffer.IsCreated) _gridBuffer.Dispose();
            if (_gridTempBuffer.IsCreated) _gridTempBuffer.Dispose();
            if (_covBuffer.IsCreated) _covBuffer.Dispose();
            if (_gridLut.IsCreated) _gridLut.Dispose();
            if (_blockBuffer.IsCreated) _blockBuffer.Dispose();
            if (_blockColorBuffer.IsCreated) _blockColorBuffer.Dispose();
            if (_frozenOffsets.IsCreated) _frozenOffsets.Dispose();
            if (_bubblesBuffer.IsCreated) _bubblesBuffer.Dispose();
            if (_bubblesPoolBuffer.IsCreated) _bubblesPoolBuffer.Dispose();
            if (_componentsBuffer.IsCreated) _componentsBuffer.Dispose();
            if (_gridIDBuffer.IsCreated) _gridIDBuffer.Dispose();
            if (_controllerBuffer.IsCreated) _controllerBuffer.Dispose();
            if (_lastControllerBuffer.IsCreated) _lastControllerBuffer.Dispose();
            if (_slimeInstances.IsCreated) _slimeInstances.Dispose();
            if (_colliderBuffer.IsCreated)  _colliderBuffer.Dispose();

            _marchingCubes.Dispose();
            _particlePosBuffer.Release();
            _particleCovBuffer.Release();
            _bubblesDataBuffer.Release();

        }

        void Update()
        {
            if (isFog && !_wasFog)
            {
                _wasFog = true;
                if (fogParticles != null)
                {
                    fogParticles.gameObject.SetActive(true); 
                    fogParticles.Play();
                }
                // Set smaller radius for fog form
                if (TryGetComponent<SphereCollider>(out var sc))
                    sc.radius = fogSphereRadius;
            }
            else if (!isFog && _wasFog)
            {
                _wasFog = false;
                if (fogParticles != null) fogParticles.Stop();
                // Restore normal radius
                if (TryGetComponent<SphereCollider>(out var sc))
                    sc.radius = normalSphereRadius;
            }

            if (isFog)
            {
                trans.rotation = Quaternion.identity;
                if (TryGetComponent<Rigidbody>(out var rb)) rb.angularVelocity = Vector3.zero;
            }

            HandleMouseInteraction();

            if (renderMode == RenderMode.Particles)
            {
#if !UNITY_WEBGL
                if (!isFog) Graphics.DrawMeshInstancedProcedural(particleMesh, 0, particleMat, _bounds, PBF_Utils.Num);
#endif
            }
            else if (renderMode == RenderMode.Surface)
            {
                if (_mesh != null && !isFog)
                {
                    // Use frozenMat when freeze factor crosses halfway, otherwise the normal mat
                    Material drawMat = (frozenMat != null && _freezeFactor >= 0.5f) ? frozenMat : mat;
                    Graphics.DrawMesh(_mesh, Matrix4x4.TRS(_bounds.min, Quaternion.identity, Vector3.one), drawMat, 0);
                }

#if !UNITY_WEBGL
                if (!isFog) Graphics.DrawMeshInstancedProcedural(particleMesh, 0, bubblesMat, _bounds, PBF_Utils.BubblesCount);
#endif
            }

            if (concentration > 5 && !isFog)
            {
                foreach (var slime in _slimeInstances)
                {
                    if (!slime.Active || slime.Dir.sqrMagnitude < 0.001f) continue;

                    Graphics.DrawMesh(faceMesh, Matrix4x4.TRS(slime.Pos * PBF_Utils.Scale,
                        Quaternion.LookRotation(-slime.Dir),
                        0.2f * math.sqrt(slime.Radius * PBF_Utils.Scale) * Vector3.one), faceMat, 0);
                }
            }
        }

        private void FixedUpdate()
        {
            ProcessFreezing();
            
            if (isFrozen && !_wasFrozen)
            {
                _wasFrozen = true;
                new Simulation_PBF.RecordOffsetsJob
                {
                    Ps = _particles,
                    FrozenOffsets = _frozenOffsets,
                    Center = (float3)trans.position * PBF_Utils.InvScale,
                    Rotation = trans.rotation,
                }.Schedule(_particles.Length, batchCount).Complete();
                
                // Swap spherical liquid collider for a convex hull of the frozen shape
                if (TryGetComponent<SphereCollider>(out var sc))
                    sc.enabled = false;

                Transform frozenColl = trans.Find("FrozenCollider");
                if (frozenColl == null)
                {
                    var go = new GameObject("FrozenCollider");
                    go.transform.SetParent(trans, false);
                    frozenColl = go.transform;
                }
                
                frozenColl.position = _bounds.min;
                frozenColl.rotation = Quaternion.identity;
                
                var mc = frozenColl.GetComponent<MeshCollider>();
                if (mc == null) mc = frozenColl.gameObject.AddComponent<MeshCollider>();
                
                mc.convex = true;
                
                // Apply zero friction physics material to prevent wall hopping in ice form
                PhysicsMaterial frozenMaterial = new PhysicsMaterial("FrozenIce")
                {
                    dynamicFriction = 0f,
                    staticFriction = 0f,
                    bounciness = 0.1f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounceCombine = PhysicsMaterialCombine.Minimum
                };
                mc.material = frozenMaterial;
                
                if (_mesh != null)
                {
                    mc.sharedMesh = _mesh;
                    mc.enabled = true;
                }
                // Note: Unity may warn about >256 polygons but still produces a valid partial hull.
            }
            else if (!isFrozen && _wasFrozen)
            {
                _wasFrozen = false;
                
                Transform frozenColl = trans.Find("FrozenCollider");
                if (frozenColl != null)
                {
                    var mc = frozenColl.GetComponent<MeshCollider>();
                    if (mc != null) mc.enabled = false;
                }
                    
                if (TryGetComponent<SphereCollider>(out var sc))
                    sc.enabled = true;
            }

            // 1 PBF iteration keeps simulation cost low.
            Profiler.BeginSample("Simulate");
            Simulate();
            Profiler.EndSample();

            Surface();
            
            Control();
            
#if !UNITY_WEBGL
            Bubbles();
#endif
            
            bubblesNum = PBF_Utils.BubblesCount - _bubblesPoolBuffer.Length;
            
            if (renderMode == RenderMode.Particles)
            {
                _particlePosBuffer.SetData(_particles);
                particleMat.SetInt("_Aniso", 0);
            }
            else
                _bubblesDataBuffer.SetData(_bubblesBuffer);
            
            _bounds = new Bounds()
            {
                min = minPos * PBF_Utils.Scale,
                max = maxPos * PBF_Utils.Scale
            };
        }

        private void Surface()
        {
            Profiler.BeginSample("Render");

            var handle = new Reconstruction.ComputeMeanPosJob
            {
                Lut = _lut,
                Ps = _particles,
                MeanPos = _particlesTemp,
            }.Schedule(_particles.Length, batchCount);

            if (useAnisotropic)
            {
                handle = new Reconstruction.ComputeCovarianceJob
                {
                    Lut = _lut,
                    Ps = _particles,
                    MeanPos = _particlesTemp,
                    GMatrix = _covBuffer,
                }.Schedule(_particles.Length, batchCount, handle);
            }

            new Reconstruction.CalcBoundsJob()
            {
                Ps = _particles,
                Bounds = _boundsBuffer,
            }.Schedule(handle).Complete();

            Profiler.EndSample();

            _gridLut.Clear();
            float blockSize = PBF_Utils.CellSize * 4;
            minPos = math.floor(_boundsBuffer[0] / blockSize) * blockSize;
            maxPos = math.ceil(_boundsBuffer[1] / blockSize) * blockSize;

            Profiler.BeginSample("Allocate");
            handle = new Reconstruction.ClearGridJob
            {
                Grid = _gridBuffer,
                GridID = _gridIDBuffer,
            }.Schedule(_gridBuffer.Length, batchCount);

            handle = new Reconstruction.AllocateBlockJob()
            {
                Ps = _particlesTemp,
                GridLut = _gridLut,
                MinPos = minPos,
            }.Schedule(handle);
            handle.Complete();

            var keys = _gridLut.GetKeyArray(Allocator.TempJob);
            blockNum = keys.Length;

            new Reconstruction.ColorBlockJob()
            {
                Keys = keys,
                Blocks = _blockBuffer,
                BlockColors = _blockColorBuffer,
            }.Schedule().Complete();

            Profiler.EndSample();

            Profiler.BeginSample("Splat");

#if USE_SPLAT_SINGLE_THREAD
            handle = new Reconstruction.DensityProjectionJob()
            {
                Ps = _particlesTemp,
                GMatrix = _covBuffer,
                Grid = _gridBuffer,
                GridLut = _gridLut,
                MinPos = minPos,
                UseAnisotropic = useAnisotropic,
            }.Schedule();
#elif USE_SPLAT_COLOR8
            for (int i = 0; i < 8; i++)
            {
                int2 slice = new int2(_blockColorBuffer[i], _blockColorBuffer[i + 1]);
                int count = slice.y - slice.x;
                handle = new Reconstruction.DensitySplatColoredJob()
                {
                    ParticleLut = _lut,
                    ColorKeys = _blockBuffer.Slice(slice.x, count),
                    Ps = _particlesTemp,
                    GMatrix = _covBuffer,
                    Grid = _gridBuffer,
                    GridLut = _gridLut,
                    MinPos = minPos,
                    UseAnisotropic = useAnisotropic,
                }.Schedule(count, count, handle);
            }
#else
            handle = new Reconstruction.DensityProjectionParallelJob()
            {
                Ps = _particlesTemp,
                GMatrix = _covBuffer,
                GridLut = _gridLut,
                Grid = _gridBuffer,
                ParticleLut = _lut,
                Keys = keys,
                UseAnisotropic = useAnisotropic,
                MinPos = minPos,
            }.Schedule(keys.Length, batchCount);
#endif
            handle.Complete();
            Profiler.EndSample();

            if (!isFog)
            {
                // Pass raw grid directly to marching cubes (skip blur for performance).
                Profiler.BeginSample("Marching cubes");
                _mesh = _marchingCubes.MarchingCubesParallel(keys, _gridLut, _gridBuffer, threshold, PBF_Utils.Scale * PBF_Utils.CellSize);
                Profiler.EndSample();
            }
            
            Profiler.BeginSample("CCA");
            _componentsBuffer.Clear();
            handle = new Effects.ConnectComponentBlockJob()
            {
                Keys = keys,
                Grid = _gridBuffer,
                GridLut = _gridLut,
                Components = _componentsBuffer,
                GridID = _gridIDBuffer,
                Threshold = 1e-4f,
            }.Schedule();
            
            handle = new Effects.ParticleIDJob()
            {
                GridLut = _gridLut,
                GridID = _gridIDBuffer,
                Particles = _particles,
                MinPos = minPos,
            }.Schedule(_particles.Length, batchCount, handle);
            
            handle.Complete();
            Profiler.EndSample();

            keys.Dispose();
        }

        private void Simulate()
        {
            _lut.Clear();
            new Simulation_PBF.ApplyForceJob
            {
                Ps = _particles,
                Velocity = _velocityBuffer,
                PsNew = _particlesTemp,
                Controllers = _controllerBuffer,
                Gravity = new float3(0, isFog ? fogGravity : gravity, 0),
                IsFrozen = _wasFrozen,
                FreezeFactor = _freezeFactor,
                Center = (float3)trans.position * PBF_Utils.InvScale,
                Rotation = trans.rotation,
                FrozenOffsets = _frozenOffsets,
            }.Schedule(_particles.Length, batchCount).Complete();

            new Simulation_PBF.HashJob
            {
                Ps = _particlesTemp,
                Hashes = _hashes,
            }.Schedule(_particles.Length, batchCount).Complete();

            _hashes.SortJob(new PBF_Utils.Int2Comparer()).Schedule().Complete();

            new Simulation_PBF.BuildLutJob
            {
                Hashes = _hashes,
                Lut = _lut
            }.Schedule().Complete();

            new Simulation_PBF.ShuffleJob
            {
                Hashes = _hashes,
                PsRaw = _particles,
                PsNew = _particlesTemp,
                Velocity = _velocityBuffer,
                PosOld = _posOld,
                PosPredict = _posPredict,
                VelocityOut = _velocityTempBuffer,
            }.Schedule(_particles.Length, batchCount).Complete();

            new Simulation_PBF.ComputeLambdaJob
            {
                Lut = _lut,
                PosPredict = _posPredict,
                Lambda = _lambdaBuffer,
            }.Schedule(_particles.Length, batchCount).Complete();

            new Simulation_PBF.ComputeDeltaPosJob
            {
                Lut = _lut,
                PosPredict = _posPredict,
                Lambda = _lambdaBuffer,
                PsNew = _particles,
                FreezeFactor = _freezeFactor,
            }.Schedule(_particles.Length, batchCount).Complete();

            new Simulation_PBF.UpdateJob
            {
                Ps = _particles,
                PosOld = _posOld,
                Colliders = _colliderBuffer,
                Velocity = _velocityTempBuffer,
            }.Schedule(_particles.Length, batchCount).Complete();

            new Simulation_PBF.ApplyViscosityJob
            {
                Lut = _lut,
                PosPredict = _posPredict,
                VelocityR = _velocityTempBuffer,
                VelocityW = _velocityBuffer,
                ViscosityStrength = viscosityStrength,
            }.Schedule(_particles.Length, batchCount).Complete();
        }

        private void Control()
        {
            _controllerBuffer.Clear();

            // --- Split disabled ---
            // Always treat the slime as a single body by using only the largest component.
            // This prevents controller confusion, freeze-on-split bugs, and other weird interactions.
            // Stray particles are still attracted back via the toMain velocity below.

            if (_componentsBuffer.Length == 0) return;

            // Find the largest component by CellCount
            int largestIdx = 0;
            for (int i = 1; i < _componentsBuffer.Length; i++)
            {
                if (_componentsBuffer[i].CellCount > _componentsBuffer[largestIdx].CellCount)
                    largestIdx = i;
            }

            var component = _componentsBuffer[largestIdx];
            float3 extent = component.BoundsMax - component.Center;
            float radius = math.max(1, (extent.x + extent.y + extent.z) * PBF_Utils.CellSize * 0.6f);
            float3 center = minPos + component.Center * PBF_Utils.CellSize;
            if (extent.y < 3)
                center.y += extent.y * PBF_Utils.Scale * PBF_Utils.CellSize;

            // Pull all particles (including any stray pieces) toward the main body center
            float3 diff = (float3)trans.position * PBF_Utils.InvScale - center;
            float3 toMain = math.normalizesafe(diff) * math.clamp(math.length(diff) * 1.5f, 6f, 20f);

            _controllerBuffer.Add(new ParticleController()
            {
                Center = center,
                Radius = radius * 2.0f, // Wide radius so stray particles are also gathered
                Velocity = toMain,
                Concentration = concentration,
            });
            
            RearrangeInstances();
        }

        private void RearrangeInstances()
        {
            if (_slimeInstances.Length - _instancePool.Count > _controllerBuffer.Length)
            {
                var used = new NativeArray<bool>(_slimeInstances.Length, Allocator.Temp);
                for (int controllerID = 0; controllerID < _controllerBuffer.Length; controllerID++)
                {
                    var controller = _controllerBuffer[controllerID];
                    var center = controller.Center;
                    int instanceID = -1;
                    float minDst = float.MaxValue;
                    for (int j = 0; j < _slimeInstances.Length; j++)
                    {
                        var slime = _slimeInstances[j];
                        if (used[j] || !slime.Active) continue;
                        var pos = slime.Center;
                        float dst = math.lengthsq(center - pos);
                        if (dst < minDst)
                        {
                            minDst = dst;
                            instanceID = j;
                        }
                    }
                    
                    used[instanceID] = true;
                    UpdateInstanceController(instanceID, controllerID);
                }

                for (int i = 0; i < _slimeInstances.Length; i++)
                {
                    var slime = _slimeInstances[i];
                    if (used[i] || !slime.Active) continue;
                    slime.Active = false;
                    _slimeInstances[i] = slime;
                    _instancePool.Push(i);
                }
                used.Dispose();

                if (!_slimeInstances[_controlledInstance].Active)
                {
                    float3 pos = trans.position * PBF_Utils.InvScale;
                    float minDst = float.MaxValue;
                    for (int i = 0; i < _slimeInstances.Length; i++)
                    {
                        var slime = _slimeInstances[i];
                        if (!slime.Active) continue;

                        float dst = math.lengthsq(pos - slime.Center);
                        if (dst < minDst)
                        {
                            minDst = dst;
                            _controlledInstance = i;
                        }
                    }

                    int controllerID = _slimeInstances[_controlledInstance].ControllerID;
                    UpdateInstanceController(_controlledInstance, controllerID);
                }
            }
            else
            {
                var used = new NativeArray<bool>(_controllerBuffer.Length, Allocator.Temp);
                for (int instanceID = 0; instanceID < _slimeInstances.Length; instanceID++)
                {
                    var slime = _slimeInstances[instanceID];
                    if (!slime.Active)  continue;
                    var pos = slime.Center;
                    int controllerID = -1;
                    float minDst = float.MaxValue;
                    for (int j = 0; j < _controllerBuffer.Length; j++)
                    {
                        if (used[j]) continue;
                        var cl = _controllerBuffer[j];
                        var center = cl.Center;
                        float dst = math.lengthsq(center - pos);
                        if (dst < minDst)
                        {
                            minDst = dst;
                            controllerID = j;
                        }
                    }
                    if (controllerID >= 0)
                    {
                        used[controllerID] = true;
                        UpdateInstanceController(instanceID, controllerID);
                    }
                }
                
                for (int i = 0; i < _controllerBuffer.Length; i++)
                {
                    if (used[i]) continue;
                    var controller = _controllerBuffer[i];
                    float3 dir = math.normalizesafe(
                        math.lengthsq(controller.Velocity) < 1e-3f
                            ? (float3)trans.position * PBF_Utils.InvScale - controller.Center
                            : controller.Velocity,
                        new float3(1, 0, 0));
                    new Effects.RayInsectJob
                    {
                        GridLut = _gridLut,
                        Grid = _gridBuffer,
                        Result = _boundsBuffer,
                        Threshold = threshold,
                        Pos = controller.Center,
                        Dir = dir,
                        MinPos = minPos,
                    }.Schedule().Complete();
                    
                    float3 newPos = _boundsBuffer[0];
                    if (!math.all(math.isfinite(newPos)))
                        newPos = controller.Center + dir * controller.Radius * 0.5f;
                    
                    SlimeInstance slime = new SlimeInstance()
                    {
                        Active = true,
                        Center =  controller.Center,
                        Radius = controller.Radius,
                        Dir = dir,
                        Pos = newPos,
                        ControllerID = i,
                    };
                    if (_instancePool.Count > 0)
                        _slimeInstances[_instancePool.Pop()] = slime;
                    else
                        _slimeInstances.Add(slime);
                }
                used.Dispose();
            }
        }

        private void UpdateInstanceController(int instanceID, int controllerID)
        {
            if (controllerID < 0 || controllerID >= _controllerBuffer.Length) return;
            var slime = _slimeInstances[instanceID];
            var controller = _controllerBuffer[controllerID];
            
            if (instanceID == _controlledInstance)
                controller.Velocity = _velocity * PBF_Utils.InvScale;

            slime.ControllerID = controllerID;
            float speed = 0.1f;
            slime.Radius = math.lerp(slime.Radius, controller.Radius, speed);
            slime.Center = math.lerp(slime.Center, controller.Center, speed);
            Vector3 vec = controller.Velocity;
            if (vec.sqrMagnitude > 1e-4f)
            {
                var newDir = Vector3.Slerp(slime.Dir, vec.normalized, speed);
                newDir.y = math.clamp(newDir.y, -0.2f, 0.5f);
                slime.Dir = newDir.normalized;
            }
            else
                slime.Dir = Vector3.Slerp(slime.Dir, new Vector3(slime.Dir.x, 0, slime.Dir.z), speed);
            
            new Effects.RayInsectJob
            {
                GridLut = _gridLut,
                Grid = _gridBuffer,
                Result = _boundsBuffer,
                Threshold = threshold,
                Pos = controller.Center,
                Dir = slime.Dir,
                MinPos = minPos,
            }.Schedule().Complete();
            
            float3 newPos = _boundsBuffer[0];
            if (math.all(math.isfinite(newPos)))
                slime.Pos = Vector3.Lerp(slime.Pos + vec * PBF_Utils.DeltaTime, newPos, 0.1f);
            else
                slime.Pos = controller.Center;
            
            _slimeInstances[instanceID] = slime;
            
            if (instanceID == _controlledInstance)
            {
                controller.Center = trans.position * PBF_Utils.InvScale;
                _controllerBuffer[controllerID] = controller;
            }
        }

        private void Bubbles()
        {
            var handle = new Effects.GenerateBubblesJobs()
            {
                GridLut = _gridLut,
                Keys = _blockBuffer,
                Grid = _gridBuffer,
                BubblesStack = _bubblesPoolBuffer,
                BubblesBuffer = _bubblesBuffer,
                Speed = 0.01f * bubbleSpeed,
                Threshold = threshold * 1.2f,
                BlockCount = blockNum,
                MinPos = minPos,
                Seed = (uint)Time.frameCount,
            }.Schedule();

            handle = new Effects.BubblesViscosityJob()
            {
                Lut = _lut,
                Particles = _particles,
                VelocityR = _velocityBuffer,
                BubblesBuffer = _bubblesBuffer,
                Controllers = _controllerBuffer,
                ViscosityStrength = viscosityStrength / 50,
            }.Schedule(_bubblesBuffer.Length, batchCount, handle);

            handle = new Effects.UpdateBubblesJob()
            {
                GridLut = _gridLut,
                Grid = _gridBuffer,
                BubblesStack = _bubblesPoolBuffer,
                BubblesBuffer = _bubblesBuffer,
                Threshold = threshold * 1.2f,
                MinPos = minPos,
            }.Schedule(handle);
            
            handle.Complete();
        }

        void HandleMouseInteraction()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.pKey.wasPressedThisFrame)
                    _connect = true;
                
                if (keyboard.rKey.wasPressedThisFrame)
                {
                    for (int i = 0; i < _slimeInstances.Length; i++)
                    {
                        if (!_slimeInstances[i].Active) continue;
                        _controlledInstance = i;
                        trans.position = _slimeInstances[i].Center * PBF_Utils.Scale;
                        break;
                    }
                }
            }
            
            // if (Input.GetKey(KeyCode.W))
            //     velocity += new float3(0, 0, 1);
            // if (Input.GetKey(KeyCode.S))
            //     velocity += new float3(0, 0, -1);
            // if (Input.GetKey(KeyCode.A))
            //     velocity += new float3(-1, 0, 0);
            // if (Input.GetKey(KeyCode.D))
            //     velocity += new float3(1, 0, 0);
            // if (Input.GetKeyDown(KeyCode.Space))
            //     _velocityY = new float3(0, 3, 0);
            // else
            //     _velocityY = pos.y > 1e-5f ? _velocityY + new float3(0, -5f, 0) * Time.deltaTime : float3.zero;
            _velocity = trans.GetComponent<Rigidbody>().linearVelocity;
            // pos += _velocity * Time.deltaTime;
            // pos.y = Mathf.Max(0, pos.y);
            // trans.position = pos;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(_bounds.center, _bounds.size);
            if (gridDebug)
            {
                Gizmos.color = Color.blue;
                for (var i = 0; i < blockNum; i++)
                {
                    var block = _blockBuffer[i];
                    Vector3 blockMinPos = new Vector3(block.x, block.y, block.z) * PBF_Utils.CellSize * 0.4f +
                                          _bounds.min;
                    Vector3 size = new Vector3(PBF_Utils.CellSize, PBF_Utils.CellSize, PBF_Utils.CellSize) * 0.4f;
                    Gizmos.DrawWireCube(blockMinPos + size * 0.5f, size);
                }
            }

            if (componentDebug)
            {
                Gizmos.color = Color.green;
                for (var i = 0; i < _componentsBuffer.Length; i++)
                {
                    var c = _componentsBuffer[i];
                    var size = (c.BoundsMax - c.BoundsMin) * PBF_Utils.Scale * PBF_Utils.CellSize;
                    var center = c.Center * PBF_Utils.Scale * PBF_Utils.CellSize;
                    Gizmos.DrawWireCube(_bounds.min + (Vector3)center, size);
                }
                
                for (var i = 0; i < _slimeInstances.Length; i++)
                {
                    var slime = _slimeInstances[i];
                    if (!slime.Active) continue;
                    Gizmos.DrawWireSphere(slime.Center * PBF_Utils.Scale, slime.Radius * PBF_Utils.Scale);
#if UNITY_EDITOR
                    UnityEditor.Handles.Label(slime.Center * PBF_Utils.Scale, $"id:{i}");
#endif
                    if (_connect)
                        Gizmos.DrawLine(slime.Center * PBF_Utils.Scale + new float3(0, 0.1f, 0), trans.position + new Vector3(0, 0.1f, 0));
                }

                Gizmos.color = Color.cyan;
                for (var i = 0; i < _colliderBuffer.Length; i++)
                {
                    var c = _colliderBuffer[i];
                    Gizmos.DrawWireCube(c.Center * PBF_Utils.Scale, c.Extent * PBF_Utils.Scale * 2);
                }
            }
        }
        private void ProcessFreezing()
        {
            // Smoothly transition the freeze factor (0 = liquid, 1 = frozen)
            float targetFactor = isFrozen ? 1f : 0f;
            _freezeFactor = Mathf.MoveTowards(_freezeFactor, targetFactor, Time.fixedDeltaTime * freezeSpeed);

            // Apply physics changes
            viscosityStrength = Mathf.Lerp(_originalViscosity, frozenViscosity, _freezeFactor);
            bubbleSpeed = Mathf.Lerp(_originalBubbleSpeed, 0f, _freezeFactor);
            // Material swap is handled in Update via Graphics.DrawMesh (no Renderer component on this object)
        }
    }
}
