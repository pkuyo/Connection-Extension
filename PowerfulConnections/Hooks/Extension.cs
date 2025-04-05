using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerfulConnections.Hooks
{
	public abstract class Extension<T> where T : class
	{
		public WeakReference<T> ownerRef;

		protected Extension(T owner)
		{
			ownerRef = new WeakReference<T>(owner);
		}

		public T Owner
		{
			get
			{
				if (ownerRef.TryGetTarget(out var target))
					return target;
				return null;
			}
		}

	}
}
