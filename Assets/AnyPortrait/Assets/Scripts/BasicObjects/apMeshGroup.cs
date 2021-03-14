﻿/*
*	Copyright (c) 2017-2019. RainyRizzle. All rights reserved
*	Contact to : https://www.rainyrizzle.com/ , contactrainyrizzle@gmail.com
*
*	This file is part of [AnyPortrait].
*
*	AnyPortrait can not be copied and/or distributed without
*	the express perission of [Seungjik Lee].
*
*	Unless this file is downloaded from the Unity Asset Store or RainyRizzle homepage, 
*	this file and its users are illegal.
*	In that case, the act may be subject to legal penalties.
*/

using UnityEngine;
//using UnityEngine.Profiling;
using System.Collections;
using System.Collections.Generic;
using System;

using AnyPortrait;

namespace AnyPortrait
{

	[Serializable]
	public class apMeshGroup : MonoBehaviour
	{
		// Members
		//------------------------------------
		[SerializeField]
		public int _uniqueID = -1;


		public enum PRESET_TYPE
		{
			Default = 0,
			Face = 1,
			Hair = 2,
			Body = 3,
			Weapon = 4,
			Item = 5,
			Custom = 6,
		}
		public PRESET_TYPE _presetType = PRESET_TYPE.Default;

		[SerializeField]
		public string _name = "";

		// Mesh Group의 특징
		// 1. 하위에 Mesh들을 가지고 있다.
		// 2. 각각의 Mesh는 중복이 가능하며, Transform과 Modifier를 적용할 수 있다. (Mesh는 소스에 불과)
		// 3. Mesh Group 자체도 Child MeshGroup을 가질 수 있다. [Transform을 가진다.]

		//중요


		[SerializeField]
		public List<apTransform_Mesh> _childMeshTransforms = new List<apTransform_Mesh>();

		//상위 MeshGroup
		[SerializeField]
		public int _parentMeshGroupID = -1;

		[NonSerialized]
		public apMeshGroup _parentMeshGroup = null;


		//추가!
		//Bone 리스트
		//전체 본 리스트와 Root본 리스트를 두고 관리한다.
		//Bone은 다중 루트를 지원한다.
		//Root Bone은 직렬화되지 않고 Link시에 따로 구분해준다.
		[SerializeField]
		public List<apBone> _boneList_All = new List<apBone>();

		[NonSerialized]
		public List<apBone> _boneList_Root = new List<apBone>();

		//추가 9.27 : 참조용 Bone List. 하위의 본 리스트를 포함해서 묶었다. 에디터에서는 이걸 이용하자
		/// <summary>
		/// 기존의 apBone을 리스트로 관리한 것과 달리,
		/// 하위의 Bone 리스트도 한번에 참조하기 위해서 에디터에서 한번에 사용하기 위한 통합된 Bone 리스트 래퍼 클래스
		/// </summary>
		public class BoneListSet
		{
			public bool _isRootMeshGroup = true;
			public apMeshGroup _meshGroup = null;
			public apTransform_MeshGroup _meshGroupTransform = null;
			public List<apBone> _bones_All = new List<apBone>();
			public List<apBone> _bones_Root = new List<apBone>();
		}
		
		[NonSerialized, NonBackupField]
		public List<BoneListSet> _boneListSets = new List<BoneListSet>();



		private bool _isBoneUpdatable = false;



		//하위 MeshGroup
		[SerializeField]
		public List<apTransform_MeshGroup> _childMeshGroupTransforms = new List<apTransform_MeshGroup>();


		///// <summary>
		///// 하위 MeshGroup Transform 중 "Bone"을 가지고 있는 것만 따로 추려낸 리스트.
		///// 처리 순서에 맞게 정렬된다. >> 이거 안쓰는 방향으로 설정
		///// </summary>
		//[NonSerialized]
		//public List<apTransform_MeshGroup> _childMeshGroupTransformsWithBones = new List<apTransform_MeshGroup>();


		//렌더링 순서 정렬
		//1. GUI라면 List Sort를 통해서 해야한다.
		//2. Realtime이라면 그냥 Mesh 자체를 Local Z 배치를 하면 된다.
		//Sort는 새로 추가되었을 때, 처음, Depth가 바뀐 경우에 시행한다.
		//저장되는 값은 아니다.
		//Child Mesh Group의 Depth값을 이용하여 전체 Mesh에 대한 Depth Sort를 진행한다.
		//Matrix 처리용 리스트 (계층 방식)이 있고, 렌더링을 위한 선형 리스트가 있다.
		[NonSerialized]
		public List<apRenderUnit> _renderUnits_All = new List<apRenderUnit>();


		//추가 11.30 : Extra-Depth Changed 이벤트에 의해서 다시 정렬이 필요할 때
		//이 리스트는 "Depth가 변경된 이벤트"일 때에만 사용된다. (정렬을 매번 할 필요는 없으니까..)
		[NonSerialized]
		private apSortedRenderBuffer _sortedRenderBuffer = new apSortedRenderBuffer();

		public apSortedRenderBuffer SortedBuffer {  get {  return _sortedRenderBuffer; } }

		/// <summary>
		/// 기존의 "_renderUnits_All"과 달리 Extra 옵션에 의해서 순서가 바뀌는 것이 적용되는 리스트이다.
		/// 뒤에서 부터 앞으로 정렬되어 있다.
		/// </summary>
		public List<apRenderUnit> SortedRenderUnits {  get {  return _sortedRenderBuffer.SortedRenderUnits; } }


		[NonSerialized]
		public int _maxRenderUnitLevel = -1;


		[SerializeField]//<<NonSerializeField => SerializeField로 변경
		public apTransform_MeshGroup _rootMeshGroupTransform = null;//<<자기 자신에 대한 Transform이 있어야 한다.

		[NonSerialized]
		public apRenderUnit _rootRenderUnit = null;

		[NonSerialized]
		public bool _isNeedRenderUnitReset = true;

		[NonSerialized]
		public bool _isNeedRenderUnitSort = true;

		[NonSerialized]
		public apPortrait _parentPortrait = null;//이건 초기화시에 꼭 넣어줘야 한다. [Editor / Realtime 모두]

		//모디파이어 정보
		[SerializeField]
		public apModifierStack _modifierStack = new apModifierStack();


		// 업데이트 시간
		//private int _FPS = 30;//<FPS는 바꿀 수 있다.//이건 나중에
		//private float _secPerFrame = 1.0f / 30.0f;
		private float _tDelta = 0.0f;

		private float _tUpdateBias = 0.0f;
		//private bool _isUpdateByBias = false;

		//private int _curUpdateKeyIndex = -1;//중복 업데이트를 막기위한 Key

		//딜레이 없이 바로 업데이트하는 객체를 저장하는 리스트
		//private List<apRenderUnit> _forceUpdateTargets = new List<apRenderUnit>();
		//private bool _isAnyForceUpdate = false;
		//private FUNC_IS_FORCE_UPDATE _funcForceUpdate = null;

		//추가 : IK를 업데이트에서 사용할지 여부
		//Bone IK 렌더링 + Rigging에 적용 여부를 모두 설정해야한다.
		//메인 업데이트 외에는 False로 설정된다.
		[NonSerialized]
		private bool _isUpdate_BoneIKMatrix = false;//

		[NonSerialized]
		private bool _isUpdate_BoneIKRigging = false;//
		
		public bool IsRiggingWithIK { get { return _isUpdate_BoneIKMatrix && _isUpdate_BoneIKRigging; } }

		// Init
		//------------------------------------
		void Start()
		{
			//업데이트하지 않습니다.
			this.enabled = false;
		}

		//public apMeshGroup()
		//{
		//	SetDirtyToReset();
		//}

		public void MakeRootTransform(int transformID)
		{
			//Debug.Log("MakeRootTransform : " + _name);
			_rootMeshGroupTransform = new apTransform_MeshGroup(transformID);

			_rootMeshGroupTransform._meshGroupUniqueID = this._uniqueID;
			_rootMeshGroupTransform._nickName = "Root MeshGroup";
			_rootMeshGroupTransform._meshGroup = this;
			_rootMeshGroupTransform._matrix = new apMatrix();
			_rootMeshGroupTransform._isVisible_Default = true;

			_rootMeshGroupTransform._depth = 0;


		}

		public void Init(apPortrait parentPortrait)
		{
			_parentPortrait = parentPortrait;
			_modifierStack._parentPortrait = parentPortrait;
			_modifierStack._parentMeshGroup = this;

			if (_rootMeshGroupTransform == null)
			{
				MakeRootTransform(parentPortrait.MakeUniqueID(apIDManager.TARGET.Transform));
			}

			_rootMeshGroupTransform._meshGroup = this;

			//ID를 등록해주자
			_parentPortrait.RegistUniqueID(apIDManager.TARGET.MeshGroup, _uniqueID);
			_parentPortrait.RegistUniqueID(apIDManager.TARGET.Transform, _rootMeshGroupTransform._transformUniqueID);

			_isUpdate_BoneIKMatrix = false;
			_isUpdate_BoneIKRigging = false;
		}

		public void SetDirtyToReset()
		{
			_isNeedRenderUnitReset = true;
			_isNeedRenderUnitSort = true;
		}


		public void SetDirtyToSort()
		{
			_isNeedRenderUnitSort = true;
		}

		/// <summary>
		/// 렌더링 직전에만 설정해야한다.
		/// Matrix를 계산할 지, Rigging에도 적용할지 결정한다.
		/// 단, Rigging이 True라면 Matrix 계산 여부 상관없이 Matrix는 계산된다.
		/// </summary>
		/// <param name="isEnabled"></param>
		public void SetBoneIKEnabled(bool isEnabled_Matrix, bool isEnabled_Rigging)
		{
			_isUpdate_BoneIKMatrix = isEnabled_Matrix | isEnabled_Rigging;
			_isUpdate_BoneIKRigging = isEnabled_Rigging;

			//<BONE_EDIT> 일단 이 부분 없앤다.
			//if (_childMeshGroupTransformsWithBones != null && _childMeshGroupTransformsWithBones.Count > 0)
			//{
			//	for (int i = 0; i < _childMeshGroupTransformsWithBones.Count; i++)
			//	{
			//		if (_childMeshGroupTransformsWithBones[i]._meshGroup != null)
			//		{
			//			_childMeshGroupTransformsWithBones[i]._meshGroup.SetBoneIKEnabled(isEnabled_Matrix, isEnabled_Rigging);
			//		}
			//	}
			//}

			//추가 10.4 : Child Mesh에도 적용
			if(_childMeshGroupTransforms != null && _childMeshGroupTransforms.Count > 0)
			{
				apTransform_MeshGroup childTransform = null;
				for (int i = 0; i < _childMeshGroupTransforms.Count; i++)
				{
					childTransform = _childMeshGroupTransforms[i];
					if(childTransform._meshGroup != this)
					{
						childTransform._meshGroup.SetBoneIKEnabled(isEnabled_Matrix, isEnabled_Rigging);
					}
				}
			}
			
		}

		public void BoneGUIUpdate(bool isIKUsing)
		{
			BoneListSet boneSet = null;
			for (int iSet = 0; iSet < _boneListSets.Count; iSet++)
			{
				boneSet = _boneListSets[iSet];
				for (int iRoot = 0; iRoot < boneSet._bones_Root.Count; iRoot++)
				{
					boneSet._bones_Root[iRoot].GUIUpdate(true, isIKUsing);
				}
			}
		}

		// Functions
		//----------------------------------------------------------------------------------
		void Update()
		{

		}




		// Reset / Refresh = Update
		//----------------------------------------------------------------------------------
		public void UpdateRenderUnits(float tDelta, bool isUpdateVertsAlways)
		{
			//일정 프레임마다 업데이트하도록 한다.
			_tDelta += tDelta;
			

			//Profiler.BeginSample("MeshGroup Update Per Frame");

			UpdateAllRenderUnits(_tDelta, isUpdateVertsAlways, false);

			//Profiler.EndSample();

			_tDelta = 0.0f;
			
		}


		/// <summary>
		/// 호출시 MeshGroup 전체를 업데이트를 한다.
		/// 매 프레임마다 호출시 성능이 저하되는 요인이 된다.
		/// </summary>
		/// <param name="isDepthChanged"></param>
		public void RefreshForce(bool isDepthChanged = false, float tDelta = 0.0f)
		{
			//Profiler.BeginSample("MeshGroup Refresh Force");
			UpdateAllRenderUnits(tDelta, true, isDepthChanged);

			//Profiler.EndSample();
		}


		


		/// <summary>
		/// MeshGroup의 Render Unit들을 렌더링한다.
		/// </summary>
		/// <param name="tDelta">프레임 경과 시간</param>
		/// <param name="isUpdateVertAlways">Vertex를 항상 업데이트하는가 (재생 전용인 경우에는 False를 설정)</param>
		/// <param name="isDepthChanged">Depth가 바뀐게 있어서 강제로 Refresh를 한번 해야하는가</param>
		private void UpdateAllRenderUnits(float tDelta, bool isUpdateVertAlways, bool isDepthChanged)
		{
			if (_parentPortrait == null)
			{
				return;
			}
			if (_isNeedRenderUnitReset || _renderUnits_All.Count == 0 || _rootRenderUnit == null)
			{
				ResetRenderUnits();
			}

			if (_isNeedRenderUnitSort)
			{
				//Debug.Log("Sort");
				SortRenderUnits(isDepthChanged);
			}

			
			_tUpdateBias += tDelta;
			if (_tUpdateBias > 0.01f)
			{
				_tUpdateBias = 0.0f;
			}

			//추가 11.30 : DepthChanged 이벤트
			_sortedRenderBuffer.ReadyToUpdate();
			


			//추가
			//본 업데이트 1단계
			ReadyToUpdateBones();

			//Modifier를 계산한다.
			UpdateModifierStack_Pre(tDelta);

			
			//값을 계산해준다.
			//값은 계층 방식으로 호출
			_rootRenderUnit.ReadyToUpdate();

			//CalculateParam을 계산하고 RenderUnit에 반영한다. [Pre]
			_rootRenderUnit.Update_Pre(tDelta);//Vertex + WorldMatrix가 계산된다.//<<이것도 Pre/Post로 나눠야 한다.
			
			//본 업데이트 : [WorldMatrix 생성]을 하자!
			UpdateBonesWorldMatrix();

			
			//Rigging Modifier는 여기서 계산해야하는데?
			//RenderUnit과 Bone Matrix가 계산되는게 우선되어야하는 Modifier는 여기서 업데이트한다.
			UpdateModifierStack_Post(tDelta);
			
			//CalculateParam을 계산하고 RenderUnit에 반영한다. [Post]
			_rootRenderUnit.Update_Post(tDelta);

			_rootRenderUnit.UpdateToRenderVert(tDelta, isUpdateVertAlways);//<<추가 : Update와 [적용]이 분리되었다.


			//추가 11.30
			//만약 Extra-Depth Changed 이벤트가 발생했다면, RenderUnit을 다시 재정렬 해야한다.
			//기존의 "_renderUnits_All"을 재정렬하는 것은 위험하다.
			//이때는 "_renderUnits_All_DepthChanged"을 클리어하고, 여기에 값을 넣는다.
			_sortedRenderBuffer.UpdateDepthChangedEventAndBuffers();
		}


		public void UpdateModifierStack_Pre(float tDelta)
		{
			_modifierStack.Update_Pre(tDelta);

			for (int i = 0; i < _childMeshGroupTransforms.Count; i++)
			{
				if (_childMeshGroupTransforms[i]._meshGroup != null)
				{
					_childMeshGroupTransforms[i]._meshGroup.UpdateModifierStack_Pre(tDelta);
				}
			}
		}


		/// <summary>
		/// Modifier 업데이트시 순서의 문제로 나중에 처리해야하는 타입이 있다.
		/// [1. Vertex/Transform 제어 Modifier] -> (RenderUnit Transform 갱신) -> (Bone Matrix 갱신) -> [2. Bone Matrix를 활용하는 Modifier..등] -> (Render Vert 갱신)
		/// 이 함수는 [2] 과정의 Modifier를 업데이트한다.
		/// </summary>
		/// <param name="tDelta"></param>
		public void UpdateModifierStack_Post(float tDelta)
		{
			_modifierStack.Update_Post(tDelta);

			for (int i = 0; i < _childMeshGroupTransforms.Count; i++)
			{
				if (_childMeshGroupTransforms[i]._meshGroup != null)
				{
					_childMeshGroupTransforms[i]._meshGroup.UpdateModifierStack_Post(tDelta);
				}
			}
		}

		public void SortRenderUnits(bool isDepthChanged)
		{
			_isNeedRenderUnitSort = false;

			List<apRenderUnit> sortedList = new List<apRenderUnit>();

			if (isDepthChanged)
			{
				//Depth 처리
				//Mesh Group 내부에서는 모두 되었다고 판단
				//Depth = Layer
				//1. RenderUnit의 Depth를 기준으로 먼저 Sort
				//2. RenderUnit 순서대로 Depth를 다시 설정한다. (1 ~ n)
				//>> 단, MeshGroup이 중간에 포함되면 -> 해당 MeshGroup이 포함된 RenderUnit의 마지막 Depth를 가져와야 한다.
				//3. 갱신된 RenderUnit의 Depth를 각각의 Transform에 넣어준다.


				//TODO
				//규칙 추가
				// Detph Sort는 기본적으로
				// "같은 레벨인 경우"에만 적용한다.
				// 다른 레벨인 경우는 상위 Sort 뒤에 붙으면서, 상위 레벨의 다음 유닛의 앞쪽에 붙는다.
				_maxRenderUnitLevel = -1;
				if (_rootRenderUnit != null)
				{
					RefreshRenderUnitLevel(_rootRenderUnit, 0);

					//UI와 달리 여기서는 오름차순 (먼저 출력될 것 부터 가져온다.)

					SortRenderUnitByLevelAndDepth(_rootRenderUnit, sortedList);
				}
				

				

				_renderUnits_All.Clear();
				for (int i = 0; i < sortedList.Count; i++)
				{
					_renderUnits_All.Add(sortedList[i]);
				}
				
				//1차로 Depth 값을 계산한다.
				int curDepth = 0;//<0부터 시작 (Root가 0이다)
				for (int iUnit = 0; iUnit < _renderUnits_All.Count; iUnit++)
				{
					apRenderUnit renderUnit = _renderUnits_All[iUnit];
					curDepth = renderUnit.SetDepth(curDepth);
					renderUnit.SetDepthForSort(curDepth);//<<추가
					curDepth++;
				}

				if (_rootRenderUnit != null)
				{
					RefreshDepth(_rootRenderUnit, 0);
				}

				//Clip 여부에 따라서 다시 설정
				bool isAnyClipping = RefreshAutoClipping();

				if (isAnyClipping)
				{
					//2차로 Depth 값을 계산한다.
					//여기서는 Clipping의 Parent / Child가 같이 묶이도록 한다.
					List<apRenderUnit> nextRenderUnits = new List<apRenderUnit>();
					for (int iUnit = 0; iUnit < _renderUnits_All.Count; iUnit++)
					{
						apRenderUnit renderUnit = _renderUnits_All[iUnit];
						if (nextRenderUnits.Contains(renderUnit))
						{
							continue;
						}

						nextRenderUnits.Add(renderUnit);

						if (renderUnit._unitType == apRenderUnit.UNIT_TYPE.Mesh
							&& renderUnit._meshTransform != null)
						{
							apTransform_Mesh meshTransform = renderUnit._meshTransform;

							//>>Child 위주로 수정을 하자
							if (meshTransform._isClipping_Parent)
							{
								//Clip Mask Parent Mesh라면
								//미리 Child를 넣어주자
								if (meshTransform._clipChildMeshes.Count > 0)
								{
									for (int iClip = 0; iClip < meshTransform._clipChildMeshes.Count; iClip++)
									{
										apTransform_Mesh childMesh = meshTransform._clipChildMeshes[iClip]._meshTransform;
										if (childMesh != null)
										{
											apRenderUnit childRenderUnit = GetRenderUnit(childMesh);

											meshTransform._clipChildMeshes[iClip]._renderUnit = childRenderUnit;

											if (childRenderUnit != null)
											{
												if (!nextRenderUnits.Contains(childRenderUnit))
												{
													nextRenderUnits.Add(childRenderUnit);
												}
											}
										}
									}
								}
								#region [미사용 코드]
								//for (int iClip = 0; iClip < 3; iClip++)
								//{
								//	apTransform_Mesh childMesh = meshTransform._clipChildMeshTransforms[iClip];
								//	if (childMesh != null)
								//	{
								//		apRenderUnit childRenderUnit = GetRenderUnit(childMesh);

								//		meshTransform._clipChildRenderUnits[iClip] = childRenderUnit;

								//		if (childRenderUnit != null)
								//		{
								//			if (!nextRenderUnits.Contains(childRenderUnit))
								//			{
								//				nextRenderUnits.Add(childRenderUnit);
								//			}
								//		}
								//	}
								//} 
								#endregion

							}
						}
					}
					_renderUnits_All.Clear();
					for (int i = 0; i < nextRenderUnits.Count; i++)
					{
						_renderUnits_All.Add(nextRenderUnits[i]);
					}

					//다시 Depth를 계산한다.
					curDepth = 0;//<0부터 시작 (Root가 0이므로)
					for (int iUnit = 0; iUnit < _renderUnits_All.Count; iUnit++)
					{
						//Debug.Log("Depth [" + curDepth + "]");
						apRenderUnit renderUnit = _renderUnits_All[iUnit];
						curDepth = renderUnit.SetDepth(curDepth);
						renderUnit.SetDepthForSort(curDepth);//<<추가
						curDepth++;
					}
				}
				//Depth를 다시 넣어주자
				if (_rootRenderUnit != null)
				{
					RefreshDepth(_rootRenderUnit, 0);
				}
			}


			//_renderUnits_All 에 대해서 전체 Depth에 대해서 오름차순(-5, -4, -3, ...0, 1, 2, 3...)으로 정렬한다.
			//Depth 값이 작을수록 뒤에 있기 때문에, 먼저 렌더링이 되어야 한다.
			//_renderUnits_All.Sort(delegate (apRenderUnit a, apRenderUnit b)
			//{
			//	return a._depth - b._depth;
			//});

			sortedList.Clear();
			if (_rootRenderUnit != null)
			{
				SortRenderUnitByLevelAndDepth(_rootRenderUnit, sortedList);
			}

			_renderUnits_All.Clear();
			for (int i = 0; i < sortedList.Count; i++)
			{
				apRenderUnit renderUnit = sortedList[i];
				renderUnit.SetDepthForSort(renderUnit.GetDepth());
				renderUnit.SetExtraDepthChangedEvent(OnRenderUnitExtraDepthChanged);//추가 : Extra 옵션에 의해서 Depth가 바뀌는 이벤트를 할당한다.
				_renderUnits_All.Add(renderUnit);
			}

			//추가 12.2 : Sorted Buffer에 집어넣자
			_sortedRenderBuffer.SetSortedRenderUnits(_renderUnits_All);
		}


		private void RefreshRenderUnitLevel(apRenderUnit renderUnit, int curLevel)
		{
			if (_maxRenderUnitLevel < curLevel)
			{
				_maxRenderUnitLevel = curLevel;
			}
			renderUnit._level = curLevel;
			if (renderUnit._meshGroupTransform != null)
			{
				renderUnit._meshGroupTransform._level = curLevel;
			}
			else if (renderUnit._meshTransform != null)
			{
				renderUnit._meshTransform._level = curLevel;
			}
			if (renderUnit._childRenderUnits.Count > 0)
			{
				for (int i = 0; i < renderUnit._childRenderUnits.Count; i++)
				{
					RefreshRenderUnitLevel(renderUnit._childRenderUnits[i], curLevel + 1);
				}
			}
		}

		private void SortRenderUnitByLevelAndDepth(apRenderUnit nextRenderUnit, List<apRenderUnit> resultList)
		{
			resultList.Add(nextRenderUnit);

			if (nextRenderUnit._childRenderUnits.Count > 0)
			{
				//1. 먼저 Sort를 한다.
				nextRenderUnit._childRenderUnits.Sort(delegate (apRenderUnit a, apRenderUnit b)
				{
					return a.DepthForOnlySort - b.DepthForOnlySort;
				});

				//2. 앞에서부터 리스트에 넣는데, 이걸 재귀적으로 넣는다. (Child가 Parent에 자연스럽게 묶이게 된다.)
				for (int i = 0; i < nextRenderUnit._childRenderUnits.Count; i++)
				{
					SortRenderUnitByLevelAndDepth(nextRenderUnit._childRenderUnits[i], resultList);
				}
			}
		}


		private int RefreshDepth(apRenderUnit renderUnit, int curGUIIndex)
		{

			renderUnit.RefreshDepth();
			renderUnit._guiIndex = curGUIIndex;

			if (renderUnit._childRenderUnits.Count == 0)
			{
				return curGUIIndex;
			}

			int guiIndex = curGUIIndex;
			for (int i = 0; i < renderUnit._childRenderUnits.Count; i++)
			{
				guiIndex = RefreshDepth(renderUnit._childRenderUnits[i], guiIndex + 1);
			}

			if (renderUnit._meshGroupTransform != null)
			{
				guiIndex++;
				renderUnit._guiIndex = guiIndex;
			}
			return guiIndex;
		}



		/// <summary>
		/// Clipping은 Refresh에서 Depth Sort 다음에 자동으로 다시 세팅되어야 한다.
		/// </summary>
		public bool RefreshAutoClipping()
		{
			bool isAnyClipping = false;

			//클리핑을 RenderUnit 기준으로 다시 돌리자
			//RenderUnit의 Level은 정리된 상태여야 한다.
			for (int iLevel = 0; iLevel <= _maxRenderUnitLevel; iLevel++)
			{
				List<apRenderUnit> subLevelRenderUnits = _renderUnits_All.FindAll(delegate (apRenderUnit a)
				{
					return a._meshTransform != null && a._level == iLevel;
				});

				if (subLevelRenderUnits.Count == 0)
				{
					continue;
				}


				//1. 일단 Parent는 다시 초기화 한다.
				for (int i = 0; i < subLevelRenderUnits.Count; i++)
				{
					apTransform_Mesh meshTransform = subLevelRenderUnits[i]._meshTransform;
					meshTransform.InitClipMeshAsParent();
				}

				//2. Clip-Child를 기준으로 다시 연결을 해주자
				for (int i = 0; i < subLevelRenderUnits.Count; i++)
				{
					apTransform_Mesh meshTransform = subLevelRenderUnits[i]._meshTransform;
					if (meshTransform._isClipping_Child)
					{
						isAnyClipping = true;

						//자신보다 Depth가 낮은 MeshTransform 중에서 "최대값"을 가진 MeshTransform을 찾는다.
						apTransform_Mesh maskMesh = null;
						int maxDepth = -1;
						for (int iFind = 0; iFind < subLevelRenderUnits.Count; iFind++)
						{
							apTransform_Mesh mt = subLevelRenderUnits[iFind]._meshTransform;
							if (mt == meshTransform)
							{
								continue;
							}
							if (mt._isClipping_Child)
							{
								//이미 Child라면 Pass
								continue;
							}
							if (mt._depth < meshTransform._depth)
							{
								if (maskMesh == null || maxDepth < mt._depth)
								{
									maskMesh = mt;
									maxDepth = mt._depth;
								}
							}
						}

						bool isClipAddable = false;
						if (maskMesh != null)
						{
							if (maskMesh._isClipping_Parent)
							{
								//이전 코드 : Child Clip은 최대 3
								////이미 Parent이면 -> 추가된 Child가 3 미만이어야 한다.
								//int nChildMeshes = maskMesh.GetChildClippedMeshes();
								//if (nChildMeshes < 3)
								//{
								//	isClipAddable = true;
								//}

								//변경 : 개수 제한은 없다. (성능이 딸릴뿐)
								isClipAddable = true;
							}
							else
							{
								isClipAddable = true;
							}
						}

						if (isClipAddable)
						{
							maskMesh.AddClippedChildMesh(meshTransform, GetRenderUnit(meshTransform));
						}
						else
						{
							//마땅한 Mask Mesh를 찾지 못했다.
							meshTransform._isClipping_Child = false;
							meshTransform._clipParentMeshTransform = null;
							meshTransform._clipIndexFromParent = -1;

						}
					}
				}

			}

			#region [미사용 코드] ChildMeshTransform으로만 돈다면 버그가 있다.
			////1. 일단 Parent는 다시 초기화 한다.
			//for (int i = 0; i < _childMeshTransforms.Count; i++)
			//{
			//	apTransform_Mesh meshTransform = _childMeshTransforms[i];
			//	meshTransform.InitClipMeshAsParent();
			//}

			////2. Clip-Child를 기준으로 다시 연결을 해주자
			//for (int i = 0; i < _childMeshTransforms.Count; i++)
			//{
			//	apTransform_Mesh meshTransform = _childMeshTransforms[i];
			//	if(meshTransform._isClipping_Child)
			//	{
			//		isAnyClipping = true;

			//		//자신보다 Depth가 낮은 MeshTransform 중에서 "최대값"을 가진 MeshTransform을 찾는다.
			//		apTransform_Mesh maskMesh = null;
			//		int maxDepth = -1;
			//		for (int iFind = 0; iFind < _childMeshTransforms.Count; iFind++)
			//		{
			//			apTransform_Mesh mt = _childMeshTransforms[iFind];
			//			if(mt == meshTransform)
			//			{
			//				continue;
			//			}
			//			if(mt._isClipping_Child)
			//			{
			//				//이미 Child라면 Pass
			//				continue;
			//			}
			//			if(mt._depth < meshTransform._depth)
			//			{
			//				if(maskMesh == null || maxDepth < mt._depth)
			//				{
			//					maskMesh = mt;
			//					maxDepth = mt._depth;
			//				}
			//			}
			//		}

			//		bool isClipAddable = false;
			//		if(maskMesh != null)
			//		{
			//			if(maskMesh._isClipping_Parent)
			//			{
			//				//이미 Parent이면 -> 추가된 Child가 3 미만이어야 한다.
			//				int nChildMeshes = maskMesh.GetChildClippedMeshes();
			//				if(nChildMeshes < 3)
			//				{
			//					isClipAddable = true;
			//				}
			//			}
			//			else
			//			{
			//				isClipAddable = true;
			//			}
			//		}

			//		if(isClipAddable)
			//		{
			//			maskMesh.AddClippedChildMesh(meshTransform, GetRenderUnit(meshTransform));
			//		}
			//		else
			//		{
			//			//마땅한 Mask Mesh를 찾지 못했다.
			//			meshTransform._isClipping_Child = false;
			//			meshTransform._clipParentMeshTransform = null;
			//			meshTransform._clipIndexFromParent = -1;

			//		}
			//	}
			//} 
			#endregion


			return isAnyClipping;
		}


		public void ResetRenderUnits()
		{
			_isNeedRenderUnitReset = false;

			_renderUnits_All.Clear();



			_rootRenderUnit = null;

			//AddRenderUnitPerMeshGroup(this, null, null);

			//Render Unit들을 생성해준다.
			AddRenderUnitPerMeshGroup(this, _rootMeshGroupTransform, null);

			_isNeedRenderUnitSort = true;//<<Sort가 필요하다고 알려준다.

			//중요
			//Modifier (ModMesh)와 RenderUnit의 연결을 다시 설정한다.
			_parentPortrait.LinkAndRefreshInEditor(false, null);

			//업데이트를 위한 Bone을 가진 ChildMeshGroup들을 찾아서 리스트에 넣어준다.
			LinkBoneListToChildMeshGroupsAndRenderUnits();




			//_modifierStack.ClearAllCalculateParams();
			//_modifierStack.LinkModifierStackToRenderUnitCalculateStack();
		}

		/// <summary>
		/// ResetRenderUnits() 함수의 변형. Portrait.LinkAndRefreshInEditor() 함수를 호출하지는 않는다.
		/// </summary>
		public void ResetRenderUnitsWithoutRefreshEditor()
		{
			_isNeedRenderUnitReset = false;

			_renderUnits_All.Clear();

			_rootRenderUnit = null;

			//Render Unit들을 생성해준다.
			AddRenderUnitPerMeshGroup(this, _rootMeshGroupTransform, null);

			_isNeedRenderUnitSort = true;//<<Sort가 필요하다고 알려준다.
			
			//업데이트를 위한 Bone을 가진 ChildMeshGroup들을 찾아서 리스트에 넣어준다.
			LinkBoneListToChildMeshGroupsAndRenderUnits();
		}


		/// <summary>
		/// [핵심 코드]
		/// 각 RenderUnit에 Modifier 값을 넣어준다.
		/// 이 함수가 호출되어야 Modifier의 값이 RenderUnit에 적용된다.
		/// </summary>
		public void RefreshModifierLink()
		{
			for (int i = 0; i < _renderUnits_All.Count; i++)
			{
				_renderUnits_All[i]._calculatedStack.ClearResultParams();
				_renderUnits_All[i]._calculatedStack.ResetRenderVerts();
			}
			
			
			//_modifierStack.ClearAllCalculateParams();//이전
			
			//변경 9.29
			//하위 메시 그룹의 CalculateParam들도 Clear해야한다.
			ClearAllCalculateParamsOfModifierStack_Recursive();

			//Debug.LogWarning("----------------------------------------------------------");
			//Debug.LogWarning("----- RefreshModifierLink ------ [" + _name + "] ----------");

			_modifierStack.LinkModifierStackToRenderUnitCalculateStack();

			//Debug.LogWarning("---------------------------------------------------------->>");
		}

		public void ClearAllCalculateParamsOfModifierStack_Recursive()
		{
			_modifierStack.ClearAllCalculateParams();

			if(_childMeshGroupTransforms != null && _childMeshGroupTransforms.Count > 0)
			{
				for (int i = 0; i < _childMeshGroupTransforms.Count; i++)
				{
					apTransform_MeshGroup childMeshGroupTransform = _childMeshGroupTransforms[i];
					if(childMeshGroupTransform == null || childMeshGroupTransform._meshGroup == null)
					{
						continue;
					}
					if(childMeshGroupTransform._meshGroup == this)
					{
						continue;
					}
					childMeshGroupTransform._meshGroup.ClearAllCalculateParamsOfModifierStack_Recursive();
				}
			}
		}



		public void AddRenderUnitPerMeshGroup(apMeshGroup targetMeshGroup, apTransform_MeshGroup targetMeshGroupTransform, apRenderUnit parentRenderUnit)
		{
			//1. 그룹 노드를 만든다. -> 이게 Mesh의 Parent
			string renderKeyword = "Node";
			if (parentRenderUnit == null)
			{
				renderKeyword = "Root Node";
			}

			apRenderUnit curRenderUnit_Group = null;

			//생성하기 전에
			//동일한 targetMeshGroup을 가진 다른 RenderUnit이 있는지 검색하자
			//if(_parentPortrait != null)
			//{
			//	for (int i = 0; i < _parentPortrait._meshGroups.Count; i++)
			//	{
			//		apMeshGroup curMG = _parentPortrait._meshGroups[i];
			//		if(curMG == this)
			//		{
			//			continue;
			//		}
			//		apRenderUnit findRenderUnit = curMG.GetRenderUnit(targetMeshGroupTransform);
			//		if(findRenderUnit != null)
			//		{
			//			curRenderUnit_Group = findRenderUnit;
			//			Debug.LogWarning("Re-Link Render Unit : " + curRenderUnit_Group.Name);
			//			break;
			//		}
			//	}
			//}


			if (curRenderUnit_Group == null)
			{
				curRenderUnit_Group = new apRenderUnit(_parentPortrait, renderKeyword);
				curRenderUnit_Group.SetGroup(targetMeshGroup, targetMeshGroupTransform, parentRenderUnit);
			}

			if (parentRenderUnit == null)
			{
				_rootRenderUnit = curRenderUnit_Group;
			}
			_renderUnits_All.Add(curRenderUnit_Group);

			//2. Mesh 노드를 만든다.
			for (int i = 0; i < targetMeshGroup._childMeshTransforms.Count; i++)
			{
				apTransform_Mesh meshTransform = targetMeshGroup._childMeshTransforms[i];
				if (meshTransform._mesh == null)
				{
					continue;
				}

				apRenderUnit renderUnit = null;
				//생성하기 전에
				//동일한 targetMeshGroup을 가진 다른 RenderUnit이 있는지 검색하자
				//if (_parentPortrait != null)
				//{
				//	for (int iMG = 0; iMG < _parentPortrait._meshGroups.Count; iMG++)
				//	{
				//		apMeshGroup curMG = _parentPortrait._meshGroups[iMG];
				//		if (curMG == this)
				//		{
				//			continue;
				//		}
				//		apRenderUnit findRenderUnit = curMG.GetRenderUnit(meshTransform);
				//		if (findRenderUnit != null)
				//		{
				//			renderUnit = findRenderUnit;
				//			Debug.LogWarning("Re-Link Render Unit : " + renderUnit.Name);
				//			break;
				//		}
				//	}
				//}

				if (renderUnit == null)
				{
					renderUnit = new apRenderUnit(_parentPortrait, "Mesh");
					renderUnit.SetMesh(targetMeshGroup, meshTransform, curRenderUnit_Group);
				}

				_renderUnits_All.Add(renderUnit);
			}

			//3. 하위의 Mesh Group들에 대해서 재귀 호출
			for (int i = 0; i < targetMeshGroup._childMeshGroupTransforms.Count; i++)
			{
				apTransform_MeshGroup meshGroupTransform = targetMeshGroup._childMeshGroupTransforms[i];

				if (meshGroupTransform._meshGroup == null)
				{
					continue;
				}

				AddRenderUnitPerMeshGroup(meshGroupTransform._meshGroup, meshGroupTransform, curRenderUnit_Group);
			}
		}


		public apRenderUnit GetRenderUnit(apTransform_Mesh meshTransform)
		{
			return _renderUnits_All.Find(delegate (apRenderUnit a)
			{
				return a._meshTransform == meshTransform;
			});
		}

		public apRenderUnit GetRenderUnit(apTransform_MeshGroup meshGroupTransform)
		{
			return _renderUnits_All.Find(delegate (apRenderUnit a)
			{
				return a._meshGroupTransform == meshGroupTransform;
			});
		}

		public apRenderUnit GetRenderUnit_NoRecursive(apTransform_Mesh meshTransform)
		{
			return _renderUnits_All.Find(delegate (apRenderUnit a)
			{
				return a._meshTransform == meshTransform && a._meshGroup == this;
			});
		}

		public apRenderUnit GetRenderUnit_NoRecursive(apTransform_MeshGroup meshGroupTransform)
		{
			return _renderUnits_All.Find(delegate (apRenderUnit a)
			{
				return a._meshGroupTransform == meshGroupTransform && a._meshGroup == this;
				;
			});
		}


		public bool IsContainMeshTransform(apTransform_Mesh meshTransform)
		{
			return _childMeshTransforms.Contains(meshTransform);
		}

		public apMeshGroup FindParentMeshGroupOfMeshTransform(apTransform_Mesh meshTransform)
		{
			if (IsContainMeshTransform(meshTransform))
			{
				return this;
			}

			for (int i = 0; i < _childMeshGroupTransforms.Count; i++)
			{
				apTransform_MeshGroup childMeshGroupTransform = _childMeshGroupTransforms[i];
				if (childMeshGroupTransform._meshGroup != null)
				{
					apMeshGroup resultMeshGroup = childMeshGroupTransform._meshGroup.FindParentMeshGroupOfMeshTransform(meshTransform);
					if (resultMeshGroup != null)
					{
						return resultMeshGroup;
					}
				}
			}

			return null;
		}

		public apTransform_MeshGroup FindChildMeshGroupTransform(apMeshGroup childMeshGroup)
		{

			for (int i = 0; i < _childMeshGroupTransforms.Count; i++)
			{
				apTransform_MeshGroup childMeshGroupTransform = _childMeshGroupTransforms[i];
				if (childMeshGroupTransform._meshGroup != null)
				{
					if (childMeshGroupTransform._meshGroup == childMeshGroup)
					{
						return childMeshGroupTransform;
					}

					apTransform_MeshGroup result = childMeshGroupTransform._meshGroup.FindChildMeshGroupTransform(childMeshGroup);
					if (result != null)
					{
						return result;
					}
				}
			}
			return null;
		}

		// Dpeth / Layer 관련 처리
		//-----------------------------------------------------------------------
		public void ChangeRenderUnitDepth(apRenderUnit renderUnit, int nextDepth)
		{
			if(renderUnit.GetDepth() == nextDepth)
			{
				return;
			}
			//Debug.Log("Next Depth >> : " + nextDepth);

			//추가
			//변경된 NextDepth에 대해서
			//같은 레벨의 RenderUnit 중에서 해당사항이 없을 경우
			//>> nextDepth를 증감하여 다른 RenderUnit과 교체할 수 있도록 만든다.

			//변경 : 레벨이 같은게 아니라 Parent가 같은 걸 찾아야 한다.
			//renderUnit._parentRenderUnit
			List<apRenderUnit> sameLevelRenderUnits = _renderUnits_All.FindAll(delegate (apRenderUnit a)
			{
				//return (a != renderUnit) && (a._level == renderUnit._level);//이전
				return (a != renderUnit) && (a._parentRenderUnit == renderUnit._parentRenderUnit);
			});

			

			bool isIncrease = false;

			//if (renderUnit._depth == nextDepth || sameLevelRenderUnits.Count == 0)//이전
			if (renderUnit.GetDepth() == nextDepth || sameLevelRenderUnits.Count == 0)
			{
				//이동 불가
				//Debug.LogError("이동 불가");
				SetDirtyToSort();
				SortRenderUnits(true);
				return;
			}

			int curDepth = renderUnit.GetDepth();

			//if (renderUnit._depth < nextDepth) { isIncrease = true; }//이전
			if (curDepth < nextDepth) { isIncrease = true; }
			else { isIncrease = false; }

			//변경
			//>일단 오름차순으로 정렬
			sameLevelRenderUnits.Sort(delegate(apRenderUnit a, apRenderUnit b)
			{
				return a.GetDepth() - b.GetDepth();
			});

			//시작지점을 찾는다.
			//증가시 : CurDepth보다 큰 최소값 Cur - Start ->>>
			//감소시 : CurDepth보다 작은 최대값 <<<- Start - Cur
			int iStartCheck = -1;
			int startDepth = -1;
			apRenderUnit startUnit = null;
			for (int i = 0; i < sameLevelRenderUnits.Count; i++)
			{
				apRenderUnit checkUnit = sameLevelRenderUnits[i];
				int checkDepth = checkUnit.GetDepth();
				if(isIncrease)
				{
					if(checkDepth > curDepth)
					{
						if(startUnit == null || checkDepth < startDepth)
						{
							iStartCheck = i;
							startUnit = checkUnit;
							startDepth = checkDepth;
						}
					}
				}
				else
				{
					if(checkDepth < curDepth)
					{
						if(startUnit == null || checkDepth > startDepth)
						{
							iStartCheck = i;
							startUnit = checkUnit;
							startDepth = checkDepth;
						}
					}
				}
			}

			//변경점 : +- 1일때, 만약 다른 레벨에 의해서 점프되는 경우가 있다. 그러면 더이상 이동이 안된다.
			//범위를 벗어나는 건 괜찮다. 아래에서 처리가 될 것이다.
			//즉, Depth의 변화량에서 움직임이 0일때, 최소한 움직임이 1이 되는 Depth로 바꾸어야 한다.
			//이전에는 이동 범위를 제한했지만, 이번엔 바뀜
			if(startUnit != null)
			{
				//여기서는 "이동 범위내에 속하는 RenderUnit이 있는지" 확인하는 것
				//하나라도 있으면 오케이
				bool isReplacable = false;
				if(isIncrease)
				{	
					for (int i = iStartCheck; i < sameLevelRenderUnits.Count; i++)
					{
						//iStartCheck부터 증가
						apRenderUnit checkUnit = sameLevelRenderUnits[i];
						if(checkUnit.GetDepth() <= nextDepth)
						{
							//Cur < 대상 < Next 사이에 있다면 오케이
							isReplacable = true;
							break;
						}
					}
				}
				else
				{
					for (int i = iStartCheck; i >= 0; i--)
					{
						//iStartCheck부터 감소
						apRenderUnit checkUnit = sameLevelRenderUnits[i];
						if(nextDepth <= checkUnit.GetDepth())
						{
							//Next < 대상 < Cur 사이에 있다면 오케이
							isReplacable = true;
							break;
						}
					}
				}
				if(!isReplacable)
				{
					//만약 교체할게 없다면
					//최소 한개는 교체해야한다.
					nextDepth = startUnit.GetDepth();
					//Debug.LogError("Next Depth의 최소 이동치를 보정한다. : " + nextDepth);
				}
			}

			#region [미사용 코드 : 잘못된 Next Depth 보정]
			//Debug.Log((isIncrease ? "Increase" : "Decrease") + " : " + curDepth + " > " + nextDepth);

			//if (!sameLevelRenderUnits.Exists(delegate (apRenderUnit a)
			// {
			//	 //return a._depth == nextDepth;//이전
			//	 return a.GetDepth() == nextDepth;
			// }))
			//{

			//	Debug.LogWarning("목표한 NextDepth에 해당하는 RenderUnit이 없다. >> " + nextDepth);

			//	//해당 nextDepth에 마땅한 객체가 없을 경우 검색
			//	//int optDepth = nextDepth;
			//	int optDepth = curDepth;//<<시작 지점 변경

			//	apRenderUnit optUnit = null;
			//	for (int i = 0; i < sameLevelRenderUnits.Count; i++)
			//	{
			//		apRenderUnit nextUnit = sameLevelRenderUnits[i];
			//		if (isIncrease)
			//		{
			//			//이전
			//			//if (renderUnit.GetDepth() < nextUnit.GetDepth())
			//			//{
			//			//	//목표보다는 크지만, 그 중 최소값을 찾는다.
			//			//	//if (optUnit == null || nextUnit._depth < optDepth)//이전
			//			//	if (optUnit == null || nextUnit.GetDepth() < optDepth)
			//			//	{
			//			//		optUnit = nextUnit;
			//			//		//optDepth = nextUnit._depth;//이전
			//			//		optDepth = nextUnit.GetDepth();
			//			//	}
			//			//}

			//			//변경
			//			//현재 < .... Opt[Max] < Next
			//			if (curDepth < nextUnit.GetDepth() && nextUnit.GetDepth() <= nextDepth)
			//			{
			//				//목표 범위 안에 있으며, 최대값을 찾는다.
			//				if (optUnit == null || nextUnit.GetDepth() > optDepth)
			//				{
			//					optUnit = nextUnit;
			//					//optDepth = nextUnit._depth;//이전
			//					optDepth = nextUnit.GetDepth();
			//				}
			//			}
			//		}
			//		else
			//		{
			//			//이전
			//			//if (renderUnit.GetDepth() > nextUnit.GetDepth())
			//			//{
			//			//	//목표보다는 작지만, 그 중 최대값을 찾는다.
			//			//	//if (optUnit == null || nextUnit._depth > optDepth)
			//			//	if (optUnit == null || nextUnit.GetDepth() > optDepth)
			//			//	{
			//			//		optUnit = nextUnit;
			//			//		//optDepth = nextUnit._depth;
			//			//		optDepth = nextUnit.GetDepth();
			//			//	}
			//			//}

			//			//변경
			//			//Next < Opt[Min]... < 현재
			//			if (nextDepth <= nextUnit.GetDepth() && nextUnit.GetDepth() < curDepth)
			//			{
			//				//목표 범위 안에 있으며, 최소값을 찾는다.
			//				if (optUnit == null || nextUnit.GetDepth() < optDepth)
			//				{
			//					optUnit = nextUnit;
			//					//optDepth = nextUnit._depth;//이전
			//					optDepth = nextUnit.GetDepth();
			//				}
			//			}
			//		}
			//	}
			//	if (optUnit != null)
			//	{
			//		nextDepth = optDepth;
			//		if(!isIncrease)
			//		{
			//			nextDepth--;
			//		}
			//		Debug.LogError("Next Depth가 바뀌었다.  >> : " + nextDepth);
			//	}
			//	else
			//	{
			//		Debug.LogError("Opt Depth를 찾지 못했다.  >> : " + nextDepth);
			//	}
			//}
			//else
			//{
			//	Debug.Log("목표한 NextDepth에 해당하는 RenderUnit이 있다. >> " + nextDepth);
			//} 
			#endregion

			//Debug.Log("Next Depth : " + nextDepth);
			int movedDepthOffset = Mathf.Abs(renderUnit.GetDepth() - nextDepth) + 100;


			//Debug.Log("ChangeRenderUnitDetph [" + nextDepth + "]");
			//int prevDepth = renderUnit._depth;
			int prevDepth = renderUnit.GetDepth();
			if (prevDepth < nextDepth)
			{
				//밑에서 올라왔을 경우
				//		┌--->
				//	□□■□[□]   ◇◇◇◇◇

				// (-1)
				//<<□□  □□ <[ ■ ] ◇◇◇◇◇
				for (int i = 0; i < _renderUnits_All.Count; i++)
				{
					apRenderUnit unit = _renderUnits_All[i];
					if (unit == renderUnit)
					{
						continue;
					}

					//if (unit._depth <= nextDepth)
					if (unit.GetDepth() <= nextDepth)
					{
						//이전
						//unit.SetDepth(unit.GetDepth() - 100);//<여유있게..

						//변경
						int changedDepth = unit.GetDepth() - movedDepthOffset;//100대신 더 여유있는 movedDepthOffset를 이용하자
						unit.SetDepth(changedDepth);
						unit.SetDepthForSort(changedDepth);
					}
				}
			}
			else if (prevDepth > nextDepth)
			{
				//위에서 내려왔을 경우
				//			    <-----┐		
				//	◇◇◇◇◇ [□]□■□□

				//                  (+1)
				//  ◇◇◇◇◇ [ ■ ] >  □□  □□>>

				for (int i = 0; i < _renderUnits_All.Count; i++)
				{
					apRenderUnit unit = _renderUnits_All[i];
					if (unit == renderUnit)
					{
						continue;
					}

					//if (unit._depth >= nextDepth)
					if (unit.GetDepth() >= nextDepth)
					{
						//이전
						//unit.SetDepth(unit.GetDepth() + 100);//<여유있게..

						//변경
						int changedDepth = unit.GetDepth() + movedDepthOffset;//100대신 더 여유있는 movedDepthOffset를 이용하자
						unit.SetDepth(changedDepth);
						unit.SetDepthForSort(changedDepth);
					}
				}
			}
			//renderUnit._depth = nextDepth;
			
			renderUnit.SetDepth(nextDepth);
			renderUnit.SetDepthForSort(nextDepth);//<<추가
			//나머지는 Sort가 알아서 할 것이다.


			SetDirtyToSort();
			SortRenderUnits(true);

		}

		public int GetLastDepth()
		{
			int maxDepth = 0;
			for (int i = 0; i < _renderUnits_All.Count; i++)
			{
				apRenderUnit unit = _renderUnits_All[i];
				int curLastDepth = unit.GetLastDepth();
				if (maxDepth < curLastDepth)
				{
					maxDepth = curLastDepth;
				}
			}
			return maxDepth;
		}



		// 추가 11.30 : Extra Option 관련 처리
		//------------------------------------------------------------------------------------
		private void OnRenderUnitExtraDepthChanged(apRenderUnit renderUnit, int deltaDepth)
		{
			if(deltaDepth == 0)
			{
				return;
			}

			//Debug.Log("OnRenderUnitExtraDepthChanged : " + renderUnit.Name + " / " + deltaDepth);
			_sortedRenderBuffer.OnExtraDepthChanged(renderUnit, deltaDepth);
		}

		

		// Root Mesh Group을 기준으로 "복제된 RenderUnit"을 다시 Link하는 기능
		//-------------------------------------------------------------------------------------
		/// <summary>
		/// 에디터에선 MeshGroup 출력시 RenderUnit을 공유하는게 아니라 복제하여 사용한다.
		/// 따라서 ModifiedMesh가 Link된 RenderUnit을 "현재 렌더링된 MeshGroup" 기준으로 갱신할 필요가 있다
		/// </summary>
		public void LinkModMeshRenderUnits()
		{
			//Debug.Log("<< LinkRenderUnitsToRootMeshGroup [" + _name + "] >>");
			LinkModMeshRenderUnitsToRootUnit(this);

		}


		private void LinkModMeshRenderUnitsToRootUnit(apMeshGroup rootUnit)
		{
			//Debug.Log("< LinkModMeshRenderUnitsToRootUnit : " + this._name + " (Root Unit : " + rootUnit._name + ")");
			for (int iMod = 0; iMod < _modifierStack._modifiers.Count; iMod++)
			{
				apModifierBase modifier = _modifierStack._modifiers[iMod];
				for (int iPSG = 0; iPSG < modifier._paramSetGroup_controller.Count; iPSG++)
				{
					apModifierParamSetGroup paramSetGroup = modifier._paramSetGroup_controller[iPSG];

					for (int iPS = 0; iPS < paramSetGroup._paramSetList.Count; iPS++)
					{
						apModifierParamSet paramSet = paramSetGroup._paramSetList[iPS];

						for (int iMesh = 0; iMesh < paramSet._meshData.Count; iMesh++)
						{
							apModifiedMesh modMesh = paramSet._meshData[iMesh];

							apRenderUnit renderUnit = null;
							if (modMesh._isMeshTransform)
							{
								apTransform_Mesh meshTransform = rootUnit.GetMeshTransformRecursive(modMesh._transformUniqueID);
								if (meshTransform != null)
								{
									//if(modMesh._transform_Mesh != meshTransform)
									//{
									//	Debug.LogWarning("Link Transform Changed [Mesh Transform] : " + meshTransform._nickName);
									//}
									modMesh._transform_Mesh = meshTransform;

									renderUnit = rootUnit.GetRenderUnit(meshTransform);



								}
							}
							else
							{
								apTransform_MeshGroup meshGroupTransform = rootUnit.GetMeshGroupTransformRecursive(modMesh._transformUniqueID);
								if (meshGroupTransform != null)
								{
									//if(modMesh._transform_MeshGroup != meshGroupTransform)
									//{
									//	Debug.LogWarning("Link Transform Changed [Mesh Group Transform] : " + meshGroupTransform._nickName);
									//}
									modMesh._transform_MeshGroup = meshGroupTransform;

									renderUnit = rootUnit.GetRenderUnit(meshGroupTransform);
								}
							}
							if (renderUnit != null)
							{
								//if(modMesh._renderUnit != renderUnit)
								//{
								//	Debug.LogWarning("Link Render Unit Changed : " + renderUnit.Name);
								//}

								modMesh._renderUnit = renderUnit;
							}

							//추가 : 5.19 잘못된 데이터 삭제
							modMesh.CheckAndRemoveInvalidData(modifier);

							if ((modifier.IsPhysics || modifier.IsVolume) && modMesh._isMeshTransform)
							{
								modMesh.RefreshVertexWeights(_parentPortrait, modifier.IsPhysics, modifier.IsVolume);
							}
						}

						//TODO : Bone도?
					}

				}
			}

			//Debug.Log(">> Child MeshGroup Transforms");
			for (int iChild = 0; iChild < _childMeshGroupTransforms.Count; iChild++)
			{
				apMeshGroup childMeshGroup = _childMeshGroupTransforms[iChild]._meshGroup;
				if (childMeshGroup != null && childMeshGroup != this)
				{
					childMeshGroup.LinkModMeshRenderUnitsToRootUnit(rootUnit);
				}
			}
		}

		// Get Transform
		//-----------------------------------------------------------------------
		public apTransform_Mesh GetMeshTransform(int uniqueID)
		{
			return _childMeshTransforms.Find(delegate (apTransform_Mesh a)
			{
				return a._transformUniqueID == uniqueID;
			});
		}

		public apTransform_MeshGroup GetMeshGroupTransform(int uniqueID)
		{
			return _childMeshGroupTransforms.Find(delegate (apTransform_MeshGroup a)
			{
				return a._transformUniqueID == uniqueID;
			});
		}

		/// <summary>
		/// Mesh Transform을 가져온다.
		/// 계층적으로 찾아서 하위의 하위의 MeshTranform이 있다면 그걸로 가져온다. (속도 느림)
		/// </summary>
		/// <param name="uniqueID"></param>
		/// <returns></returns>
		public apTransform_Mesh GetMeshTransformRecursive(int uniqueID)
		{
			apTransform_Mesh result = _childMeshTransforms.Find(delegate (apTransform_Mesh a)
			{
				return a._transformUniqueID == uniqueID;
			});
			if (result != null)
			{
				return result;
			}
			//만약 없다면 -> ChildMeshGroup을 뒤져서 찾아보자
			if (_childMeshGroupTransforms.Count > 0)
			{
				apMeshGroup childMeshGroup = null;
				for (int i = 0; i < _childMeshGroupTransforms.Count; i++)
				{
					childMeshGroup = _childMeshGroupTransforms[i]._meshGroup;
					if (childMeshGroup != null && childMeshGroup != this)
					{
						result = childMeshGroup.GetMeshTransformRecursive(uniqueID);
						if (result != null)
						{
							return result;
						}
					}
				}
			}
			return null;
		}

		/// <summary>
		/// MeshGroup Transform을 가져온다.
		/// 계층적으로 찾아서 하위의 하위의 MeshGroup Transform이 있다면 그걸로 가져온다. (속도 느림)
		/// </summary>
		/// <param name="uniqueID"></param>
		/// <returns></returns>
		public apTransform_MeshGroup GetMeshGroupTransformRecursive(int uniqueID)
		{
			if (_rootMeshGroupTransform != null)
			{
				if (_rootMeshGroupTransform._transformUniqueID == uniqueID)
				{
					return _rootMeshGroupTransform;
				}
			}

			apTransform_MeshGroup result = _childMeshGroupTransforms.Find(delegate (apTransform_MeshGroup a)
			{
				return a._transformUniqueID == uniqueID;
			});
			if (result != null)
			{
				return result;
			}
			//만약 없다면 -> ChildMeshGroup을 뒤져서 찾아보자
			if (_childMeshGroupTransforms.Count > 0)
			{
				apMeshGroup childMeshGroup = null;
				for (int i = 0; i < _childMeshGroupTransforms.Count; i++)
				{
					childMeshGroup = _childMeshGroupTransforms[i]._meshGroup;
					if (childMeshGroup != null && childMeshGroup != this)
					{
						result = childMeshGroup.GetMeshGroupTransformRecursive(uniqueID);
						if (result != null)
						{
							return result;
						}
					}
				}
			}
			return null;
		}

		public apRenderUnit GetRenderUnit(int transformID, bool isMeshType)
		{
			return _renderUnits_All.Find(delegate (apRenderUnit a)
			{
				if (isMeshType)
				{
					if (a._unitType == apRenderUnit.UNIT_TYPE.Mesh && a._meshTransform != null)
					{
						if (a._meshTransform._transformUniqueID == transformID)
						{
							return true;
						}
					}
				}
				else
				{
					if (a._unitType == apRenderUnit.UNIT_TYPE.GroupNode && a._meshGroupTransform != null)
					{
						if (a._meshGroupTransform._transformUniqueID == transformID)
						{
							return true;
						}
					}
				}
				return false;
			});
		}


		//추가
		/// <summary>
		/// Mesh/MeshGroup Transform을 포함하는 "바로 상위의 Parent MeshGroup"을 찾는다.
		/// Recursive한 참조가 필요한 Transform에 적절하다.
		/// Recursive가 아니라면 기본적으로는 이 MeshGroup를 리턴한다.
		/// </summary>
		/// <param name="meshTransform"></param>
		/// <param name="meshGroupTransform"></param>
		/// <returns></returns>
		
		public apMeshGroup GetSubParentMeshGroupOfTransformRecursive(apTransform_Mesh meshTransform, apTransform_MeshGroup meshGroupTransform)
		{
			if(meshTransform == null && meshGroupTransform == null)
			{
				return null;
			}
			if(meshTransform != null)
			{
				if(_childMeshTransforms.Contains(meshTransform))
				{
					//현재 자식으로 등록되어 있다.
					return this;
				}
			}
			else if(meshGroupTransform != null)
			{
				if(_childMeshGroupTransforms.Contains(meshGroupTransform))
				{
					//현재 자식으로 등록되어 있다.
					return this;
				}
			}

			//Child로 검색 시작
			for (int i = 0; i < _childMeshGroupTransforms.Count; i++)
			{
				apTransform_MeshGroup childMeshGroupTransform = _childMeshGroupTransforms[i];
				apMeshGroup result = FindSubParentMeshGroupOfTransformRecursive_WithMeshGroupTransform(meshTransform, meshGroupTransform, childMeshGroupTransform);
				if (result != null)
				{
					return result;
				}
			}

			return null;
		}

		private apMeshGroup FindSubParentMeshGroupOfTransformRecursive_WithMeshGroupTransform(apTransform_Mesh meshTransform, apTransform_MeshGroup meshGroupTransform, apTransform_MeshGroup targetMeshGroupTransform)
		{
			if(targetMeshGroupTransform._meshGroup == null)
			{
				return null;
			}
			
			if(meshTransform != null)
			{
				if(targetMeshGroupTransform._meshGroup._childMeshTransforms.Contains(meshTransform))
				{
					return targetMeshGroupTransform._meshGroup;
				}
			}
			else if(meshGroupTransform != null)
			{
				if(targetMeshGroupTransform._meshGroup._childMeshGroupTransforms.Contains(meshGroupTransform))
				{
					return targetMeshGroupTransform._meshGroup;
				}
			}

			//Child로 검색 시작
			for (int i = 0; i < targetMeshGroupTransform._meshGroup._childMeshGroupTransforms.Count; i++)
			{
				apTransform_MeshGroup childMeshGroupTransform = targetMeshGroupTransform._meshGroup._childMeshGroupTransforms[i];
				apMeshGroup result = FindSubParentMeshGroupOfTransformRecursive_WithMeshGroupTransform(meshTransform, meshGroupTransform, childMeshGroupTransform);
				if (result != null)
				{
					return result;
				}
			}
			return null;
			
		}


		// Get Modifier
		//--------------------------------------------------------------------------
		public apModifierBase GetModifier(int uniqueID)
		{
			return _modifierStack.GetModifier(uniqueID);
		}


		// 본 관련 처리
		//-----------------------------------------------------------------------------
		/// <summary>
		/// Bone에 Depth가 추가되었다.
		/// Level과 Depth에 따라 Sort한 후, 다시 인덱스에 맞게 Depth를 재설정한다.
		/// </summary>
		public void SortBoneListByLevelAndDepth()
		{
			List<apBone> sortedBones = new List<apBone>();

			_boneList_Root.Sort(delegate (apBone a, apBone b)
			{
				if(a._depth == b._depth)
				{
					return string.Compare(a._name, b._name);
				}
				return a._depth - b._depth;
			});

			//재귀적으로 Sort한다.
			for (int i = 0; i < _boneList_Root.Count; i++)
			{
				SortBoneListByLevelAndDepthRecursive(_boneList_Root[i], sortedBones, 0);
			}

			//정렬된 값을 BoneList_All에 넣고
			//Depth를 순서대로 매겨주자
			_boneList_All.Clear();
			for (int i = 0; i < sortedBones.Count; i++)
			{
				sortedBones[i]._depth = i;
				_boneList_All.Add(sortedBones[i]);
			}
		}

		/// <summary>
		/// 재귀적으로 Bone을 정렬한다. ChildBone의 순서도 바뀐다.
		/// </summary>
		/// <param name="nextBone"></param>
		/// <param name="resultList"></param>
		private void SortBoneListByLevelAndDepthRecursive(apBone nextBone, List<apBone> resultList, int level)
		{
			nextBone._level = level;
			resultList.Add(nextBone);

			if(nextBone._childBones != null && nextBone._childBones.Count > 0)
			{
				//Name과 Depth 기준으로 Child 정렬
				nextBone._childBones.Sort(delegate (apBone a, apBone b)
				{
					if (a._depth == b._depth)
					{
						return string.Compare(a._name, b._name);
					}
					return a._depth - b._depth;
				});

				//ID도 재정렬
				nextBone._childBoneIDs.Clear();
				for (int i = 0; i < nextBone._childBones.Count; i++)
				{
					nextBone._childBoneIDs.Add(nextBone._childBones[i]._uniqueID);
				}

				//Child에 대해서 Recursive하게 호출
				for (int i = 0; i < nextBone._childBones.Count; i++)
				{
					SortBoneListByLevelAndDepthRecursive(nextBone._childBones[i], resultList, level + 1);
				}
				
			}
		}


		/// <summary>
		/// Bone Depth를 바꾼다.
		/// </summary>
		/// <param name="bone"></param>
		/// <param name="nextDepth"></param>
		public void ChangeBoneDepth(apBone bone, int nextDepth)
		{
			if(bone == null)
			{
				return;
			}
			
			List<apBone> sameLevelBones = _boneList_All.FindAll(delegate (apBone a)
			{
				return (a != bone) && (a._level == bone._level);
			});

			bool isIncrease = false;
			if(bone._depth == nextDepth || sameLevelBones.Count == 0)
			{
				//이동 불가
				//단순 Sort만 하자
				SortBoneListByLevelAndDepth();
				return;
			}

			if(bone._depth < nextDepth) { isIncrease = true; }
			else						{ isIncrease = false; }

			//이제 해당 Depth를 가지는 다른 Bone과 "자리 바꿈"을 해야한다.
			if(!sameLevelBones.Exists(delegate(apBone a)
			{
				return a != bone && a._depth == nextDepth;
			}))
			{
				int optDepth = nextDepth;
				apBone optBone = null;
				for (int i = 0; i < sameLevelBones.Count; i++)
				{
					apBone nextBone = sameLevelBones[i];
					if(isIncrease)
					{
						if (bone._depth < nextBone._depth)
						{
							//목표보다는 크지만, 그 중 최소값을 찾는다.
							if (optBone == null || nextBone._depth < optDepth)
							{
								optBone = nextBone;
								optDepth = nextBone._depth;
							}
						}
					}
					else
					{
						if (bone._depth > nextBone._depth)
						{
							//목표보다는 작지만, 그 중 최대값을 찾는다.
							if (optBone == null || nextBone._depth > optDepth)
							{
								optBone = nextBone;
								optDepth = nextBone._depth;
							}
						}
					}
				}
				if(optBone != null)
				{
					nextDepth = optDepth;
				}
			}

			//이제 Depth 이동을 하자
			int prevDepth = bone._depth;
			if(prevDepth < nextDepth)
			{
				for (int i = 0; i < _boneList_All.Count; i++)
				{
					apBone curBone = _boneList_All[i];
					if(curBone == bone)
					{
						continue;
					}

					if(curBone._depth <= nextDepth)
					{
						curBone._depth -= 100;
					}
				}
			}
			else if(prevDepth > nextDepth)
			{
				for (int i = 0; i < _boneList_All.Count; i++)
				{
					apBone curBone = _boneList_All[i];
					if(curBone == bone)
					{
						continue;
					}

					if(curBone._depth >= nextDepth)
					{
						curBone._depth += 100;
					}
				}
			}

			bone._depth = nextDepth;


			//전체 Sort
			SortBoneListByLevelAndDepth();

		}




		/// <summary>
		/// 빠른 처리를 위해서 "Child MeshGroup Transform" 중에서 "Bone"을 가진 것들을 따로 추려낸다.
		/// 이후엔 RenderUnit과 Bone을 연결해준다.
		/// RenderUnit 리셋한 직후에 호출되어야 한다.
		/// </summary>
		public void LinkBoneListToChildMeshGroupsAndRenderUnits()
		{
			//<BONE_EDIT> 이 부분 대신 boneSet으로 변경
			//if (_childMeshGroupTransformsWithBones == null)
			//{
			//	_childMeshGroupTransformsWithBones = new List<apTransform_MeshGroup>();
			//}
			//_childMeshGroupTransformsWithBones.Clear();
			if (_boneListSets == null)
			{
				_boneListSets = new List<BoneListSet>();
			}
			_boneListSets.Clear();

			FindChildMeshGroupWitnBones(_rootRenderUnit);

			//Debug.Log("Bone List를 가진 Child MeshGroup을 " + _childMeshGroupTransformsWithBones.Count + "개 발견");

			//추가 : Bone <-> Render Unit 연결
			//전체 RenderUnit들 중 "MeshGroup Transform"들을 돌면서 Bone-MeshGroup(Render Unit)을 연결해준다.
			for (int iRenderUnit = 0; iRenderUnit < _renderUnits_All.Count; iRenderUnit++)
			{
				apRenderUnit renderUnit = _renderUnits_All[iRenderUnit];
				if (renderUnit._meshGroupTransform != null)
				{
					apTransform_MeshGroup meshGroupTransform = renderUnit._meshGroupTransform;
					if (meshGroupTransform._meshGroup != null)
					{
						if (meshGroupTransform._meshGroup._boneList_Root.Count > 0)
						{
							int curBoneIndex = 0;
							for (int iRootBone = 0; iRootBone < meshGroupTransform._meshGroup._boneList_Root.Count; iRootBone++)
							{
								apBone rootBone = meshGroupTransform._meshGroup._boneList_Root[iRootBone];

								//Parent RenderUnit을 연결하고 재귀적으로 연결 호출해주자
								rootBone.SetParentRenderUnit(renderUnit);
								rootBone.LinkRecursive(0);
								curBoneIndex = rootBone.SetBoneIndex(curBoneIndex) + 1;
							}
						}
					}
				}
			}
			//<BONE_EDIT>
			//_isBoneUpdatable = (_boneList_Root.Count > 0) || (_childMeshGroupTransformsWithBones.Count > 0);

			//수정된 코드
			_isBoneUpdatable = _boneList_Root.Count > 0 || _boneListSets.Count > 0;
		}

		
		private void FindChildMeshGroupWitnBones(apRenderUnit renderUnit)
		{
			
			if (renderUnit == null)
			{
				return;
			}

			List<apBone> curBoneList_All = null;
			List<apBone> curBoneList_Root = null;

			if (renderUnit._meshGroupTransform != null &&
				renderUnit._meshGroupTransform._meshGroup != null)
			{
				//<BONE_EDIT> 이전 코드
				//if (renderUnit._meshGroupTransform._meshGroup == this)
				//{
				//	//자기 자신은 제외한다.
				//}
				//else
				//{
				//	List<apBone> boneList = renderUnit._meshGroupTransform._meshGroup._boneList_All;
				//	if (boneList != null && boneList.Count > 0)
				//	{
				//		//본이 있다!
				//		_childMeshGroupTransformsWithBones.Add(renderUnit._meshGroupTransform);
				//	}
				//}

				curBoneList_All = renderUnit._meshGroupTransform._meshGroup._boneList_All;
				curBoneList_Root = renderUnit._meshGroupTransform._meshGroup._boneList_Root;

				if (curBoneList_All != null && curBoneList_All.Count > 0)
				{
					BoneListSet newSet = new BoneListSet();

					if (renderUnit._meshGroupTransform._meshGroup == this)
					{
						//자기 자신인 경우
						newSet._isRootMeshGroup = true;
					}
					else
					{
						//자식인 경우
						newSet._isRootMeshGroup = false;
					}

					newSet._meshGroup = renderUnit._meshGroupTransform._meshGroup;
					newSet._meshGroupTransform = renderUnit._meshGroupTransform;

					newSet._bones_Root.Clear();
					for (int i = 0; i < curBoneList_Root.Count; i++)
					{
						newSet._bones_Root.Add(curBoneList_Root[i]);
					}

					newSet._bones_All.Clear();
					for (int i = 0; i < curBoneList_All.Count; i++)
					{
						newSet._bones_All.Add(curBoneList_All[i]);
					}

					_boneListSets.Add(newSet);
				}
			}

			for (int i = 0; i < renderUnit._childRenderUnits.Count; i++)
			{
				FindChildMeshGroupWitnBones(renderUnit._childRenderUnits[i]);
			}

		}



		/// <summary>
		/// 본 업데이트 중 1단계. Modifier로 제어되기 전에 값을 초기화하자
		/// </summary>
		private void ReadyToUpdateBones()
		{
			//if(!_isBoneUpdatable)
			//{
			//	return;
			//}

			//<BONE_EDIT> 이전 코드
			//if (_boneList_Root.Count > 0)
			//{
			//	for (int i = 0; i < _boneList_Root.Count; i++)
			//	{
			//		_boneList_Root[i].ReadyToUpdate(true);
			//	}
			//}

			//if (_childMeshGroupTransformsWithBones.Count > 0)
			//{
			//	for (int i = 0; i < _childMeshGroupTransformsWithBones.Count; i++)
			//	{
			//		apTransform_MeshGroup transformWithBones = _childMeshGroupTransformsWithBones[i];

			//		for (int iBone = 0; iBone < transformWithBones._meshGroup._boneList_Root.Count; iBone++)
			//		{
			//			transformWithBones._meshGroup._boneList_Root[iBone].ReadyToUpdate(true);
			//		}
			//	}
			//}

			//>> 수정된 코드 : BoneListSet으로 통합
			if(_boneListSets.Count > 0)
			{
				BoneListSet boneSet = null;
				for (int iSet = 0; iSet < _boneListSets.Count; iSet++)
				{
					boneSet = _boneListSets[iSet];
					if(boneSet._bones_Root.Count > 0)
					{
						for (int iBone = 0; iBone < boneSet._bones_Root.Count; iBone++)
						{
							boneSet._bones_Root[iBone].ReadyToUpdate(true);
						}
					}
				}
			}
		}


		/// <summary>
		/// 본 업데이트를 한다. 이 함수를 호출하면 각 본의 WorldMatrix가 완성된다.
		/// </summary>
		public void UpdateBonesWorldMatrix()
		{
			if (!_isBoneUpdatable)
			{
				
				return;
			}

			//<BONE_EDIT> 이전 코드
			#region [미사용 코드 : BONE_EDIT]
			//if (_boneList_Root.Count > 0)
			//{
			//	for (int i = 0; i < _boneList_Root.Count; i++)
			//	{
			//		_boneList_Root[i].MakeWorldMatrix(true);
			//	}
			//}

			////추가
			//if(_isUpdate_BoneIKMatrix)
			//{
			//	//Bone Matrix의 IK를 업데이트 해야한다면
			//	//bool isAnyIKCalculated = false;
			//	for (int i = 0; i < _boneList_Root.Count; i++)
			//	{
			//		if(_boneList_Root[i].CalculateIK(true))
			//		{
			//			//isAnyIKCalculated = true;
			//		}
			//	}


			//	for (int i = 0; i < _boneList_Root.Count; i++)
			//	{
			//		_boneList_Root[i].MakeWorldMatrixForIK(true, false);
			//	}
			//}

			////Debug.Log("Update Bones World Matrix [" + _name + "] Child MeshGroup of Bones (" + _childMeshGroupTransformsWithBones.Count + " / " + _isBoneUpdatable + ")");

			//if (_childMeshGroupTransformsWithBones.Count > 0)
			//{
			//	for (int i = 0; i < _childMeshGroupTransformsWithBones.Count; i++)
			//	{
			//		apTransform_MeshGroup transformWithBones = _childMeshGroupTransformsWithBones[i];

			//		for (int iBone = 0; iBone < transformWithBones._meshGroup._boneList_Root.Count; iBone++)
			//		{
			//			transformWithBones._meshGroup._boneList_Root[iBone].MakeWorldMatrix(true);
			//		}

			//		if(_isUpdate_BoneIKMatrix)
			//		{
			//			//추가 : 자식 MeshGroup에 대해서도 IK 계산
			//			for (int iBone = 0; iBone < transformWithBones._meshGroup._boneList_Root.Count; iBone++)
			//			{
			//				transformWithBones._meshGroup._boneList_Root[iBone].CalculateIK(true);
			//			}

			//			for (int iBone = 0; iBone < transformWithBones._meshGroup._boneList_Root.Count; iBone++)
			//			{
			//				transformWithBones._meshGroup._boneList_Root[iBone].MakeWorldMatrixForIK(true, false);
			//			}
			//		}

			//	}
			//} 
			#endregion

			//변경 : Bone List Set 이용
			if(_boneListSets.Count == 0)
			{
				return;
			}

			BoneListSet boneSet = null;
			
			for (int iSet = 0; iSet < _boneListSets.Count; iSet++)
			{
				boneSet = _boneListSets[iSet];
				if(boneSet._bones_Root.Count == 0)
				{
					continue;
				}

				//1. World Matrix 업데이트
				for (int iRootBone = 0; iRootBone < boneSet._bones_Root.Count; iRootBone++)
				{
					boneSet._bones_Root[iRootBone].MakeWorldMatrix(true);
				}

				//2. IK가 설정된 경우, IK 업데이트
				if (_isUpdate_BoneIKMatrix)
				{
					for (int iRootBone = 0; iRootBone < boneSet._bones_Root.Count; iRootBone++)
					{
						boneSet._bones_Root[iRootBone].CalculateIK(true);
					}

					for (int iRootBone = 0; iRootBone < boneSet._bones_Root.Count; iRootBone++)
					{
						boneSet._bones_Root[iRootBone].MakeWorldMatrixForIK(true, false);
					}
				}
			}
		}

		

		/// <summary>
		/// 에디터에서 : Rigging 작업시 Rigging Test용 Matrix를 활성/비활성한다.
		/// </summary>
		/// <param name="isBoneRiggingTest"></param>
		public void SetBoneRiggingTest(bool isBoneRiggingTest)
		{
			//<BONE_EDIT>
			//for (int i = 0; i < _boneList_All.Count; i++)
			//{
			//	apBone bone = _boneList_All[i];
			//	bone.SetRiggingTest(isBoneRiggingTest);
			//}

			//>> Bone Set을 이용하자
			//모든 Bone에 적용
			BoneListSet boneSet = null;
			for (int iSet = 0; iSet < _boneListSets.Count; iSet++)
			{
				boneSet = _boneListSets[iSet];
				for (int iBone = 0; iBone < boneSet._bones_All.Count; iBone++)
				{
					boneSet._bones_All[iBone].SetRiggingTest(isBoneRiggingTest);
				}
			}
		}

		/// <summary>
		/// 에디터에서 : Rigging 작업 중인 Pose를 리셋한다.
		/// </summary>
		public void ResetRiggingTestPose()
		{
			//<BONE_EDIT>
			//for (int i = 0; i < _boneList_All.Count; i++)
			//{
			//	apBone bone = _boneList_All[i];
			//	bone.ResetRiggingTestPose();
			//}
			//for (int i = 0; i < _boneList_Root.Count; i++)
			//{
			//	apBone rootBone = _boneList_Root[i];
			//	rootBone.MakeWorldMatrix(true);
			//}

			//>> Bone Set으로 변경
			BoneListSet boneSet = null;
			for (int iSet = 0; iSet < _boneListSets.Count; iSet++)
			{
				boneSet = _boneListSets[iSet];
				for (int iBone = 0; iBone < boneSet._bones_All.Count; iBone++)
				{
					boneSet._bones_All[iBone].ResetRiggingTestPose();
				}

				for (int iRoot = 0; iRoot < boneSet._bones_Root.Count; iRoot++)
				{
					boneSet._bones_Root[iRoot].MakeWorldMatrix(true);
				}
			}
		}

		/// <summary>
		/// Bone GUI Visible을 갱신한다.
		/// </summary>
		public void RefreshBoneGUIVisible()
		{
			//<BONE_EDIT>
			//if (_boneList_Root.Count > 0)
			//{
			//	for (int i = 0; i < _boneList_Root.Count; i++)
			//	{
			//		_boneList_Root[i].RefreshGUIVisibleRecursive();
			//	}
			//}

			//>>BoneSet으로 변경
			BoneListSet boneSet = null;
			for (int iSet = 0; iSet < _boneListSets.Count; iSet++)
			{
				boneSet = _boneListSets[iSet];
				for (int iRoot = 0; iRoot < boneSet._bones_Root.Count; iRoot++)
				{
					boneSet._bones_Root[iRoot].RefreshGUIVisibleRecursive();
				}
			}
		}

		/// <summary>
		/// Bone GUI Visible을 리셋하여 모두 보이게 만든다.
		/// </summary>
		public void ResetBoneGUIVisible()
		{
			//<BONE_EDIT>
			//if (_boneList_Root.Count > 0)
			//{
			//	for (int i = 0; i < _boneList_Root.Count; i++)
			//	{
			//		_boneList_Root[i].ResetGUIVisibleRecursive();
			//	}
			//}

			//>>BoneSet으로 변경
			BoneListSet boneSet = null;
			for (int iSet = 0; iSet < _boneListSets.Count; iSet++)
			{
				boneSet = _boneListSets[iSet];
				for (int iRoot = 0; iRoot < boneSet._bones_Root.Count; iRoot++)
				{
					boneSet._bones_Root[iRoot].ResetGUIVisibleRecursive();
				}
			}
		}

		/// <summary>
		/// Bone GUI Visible을 일괄 적용 한다. 반대로 적용하고자 하는 Bone을 추가로 입력할 수 있다.
		/// </summary>
		/// <param name="isVisible"></param>
		/// <param name="exceptBone"></param>
		public void SetBoneGUIVisibleAll(bool isVisible, apBone exceptBone)
		{
			//<BONE_EDIT>
			//if (_boneList_Root.Count > 0)
			//{
			//	for (int i = 0; i < _boneList_Root.Count; i++)
			//	{
			//		_boneList_Root[i].SetGUIVisibleWithExceptBone(isVisible, true, exceptBone);
			//	}
			//}
			
			//>>BoneSet으로 변경
			BoneListSet boneSet = null;
			for (int iSet = 0; iSet < _boneListSets.Count; iSet++)
			{
				boneSet = _boneListSets[iSet];
				for (int iRoot = 0; iRoot < boneSet._bones_Root.Count; iRoot++)
				{
					boneSet._bones_Root[iRoot].SetGUIVisibleWithExceptBone(isVisible, true, exceptBone);
				}
			}
		}


		// Get Bones
		//---------------------------------------------------------------------------
		public apBone GetBone(int uniqueID)
		{
			if (_boneList_All != null && _boneList_All.Count > 0)
			{
				return _boneList_All.Find(delegate (apBone a)
				{
					return a._uniqueID == uniqueID;
				});
			}
			return null;
		}


		/// <summary>
		/// BoneID로 Bone 객체를 가져온다.
		/// Child MeshGroup의 것들도 모두 검색한다.
		/// </summary>
		/// <param name="uniqueID"></param>
		/// <returns></returns>
		public apBone GetBoneRecursive(int uniqueID)
		{
			apBone resultBone = null;
			if (_boneList_All != null && _boneList_All.Count > 0)
			{
				resultBone = _boneList_All.Find(delegate (apBone a)
				{
					return a._uniqueID == uniqueID;
				});

				if (resultBone != null)
				{
					return resultBone;
				}
			}



			//없으면 Child에서 찾자
			if (_childMeshGroupTransforms != null && _childMeshGroupTransforms.Count > 0)
			{
				for (int i = 0; i < _childMeshGroupTransforms.Count; i++)
				{
					apMeshGroup childMeshGroup = _childMeshGroupTransforms[i]._meshGroup;
					if (childMeshGroup != null)
					{
						resultBone = childMeshGroup.GetBoneRecursive(uniqueID);

						if (resultBone != null)
						{
							return resultBone;
						}
					}
				}
			}
			return null;
		}

		///// <summary>
		///// BoneID로 Bone 객체를 가져온다.
		///// Child MeshGroup의 것들도 모두 검색한다.
		///// GetBoneRecursive()과 동일한 결과를 리턴하지만 BoneSet을 활용하여 조금 더 빠르다. (아주 약간)
		///// </summary>
		///// <param name="uniqueID"></param>
		///// <returns></returns>
		//public apBone GetBoneUsingBoneSet(int uniqueID)
		//{
		//	BoneListSet boneSet = null;
		//	for (int iSet = 0; iSet < _boneListSets.Count; iSet++)
		//	{
		//		boneSet = _boneListSets[iSet];
		//		apBone resultBone = boneSet._bones_All.Find(delegate (apBone a)
		//		{
		//			return a._uniqueID == uniqueID;
		//		});
		//		if(resultBone != null)
		//		{
		//			return resultBone;
		//		}
		//	}
		//	return null;
		//}


		// GUI
		//------------------------------------
		

	}

}