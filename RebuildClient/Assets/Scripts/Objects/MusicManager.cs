using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Objects
{
	class MusicManager : MonoBehaviour
	{
		private static MusicManager instance;

		public static MusicManager Instance
		{
			get
			{
				if (instance == null)
					instance = GameObject.FindObjectOfType<MusicManager>();
				return instance;
			}
		}

		public TextAsset MapInfo;
	}
}
