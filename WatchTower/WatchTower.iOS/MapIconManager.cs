using System;
using System.Collections.Generic;
using CoreGraphics;
using UIKit;

namespace WatchTower.iOS
{
	/// <summary>
	/// This class to be used to access images for map icons.  The images that are returned
	/// are scaled appropriately, resulting in clearer images.
	/// </summary>
	public class MapIconManager
	{
		/// <summary>
		/// In-memory cache to store results of loading and scaling
		/// </summary>
		Dictionary<string, UIImage> _imageDictionary;

		const int SCALING_FACTOR = 2; // this could be a configurable setting

		public MapIconManager()
		{
			_imageDictionary = new Dictionary<string, UIImage>();
		}


		/// <summary>
		/// Gets the image associated with icon name.
		/// </summary>
		/// <returns>The image for icon name.</returns>
		/// <param name="sIconName">S icon name.</param>
		public UIImage GetImageForIconName(string sIconName)
		{
			UIImage iconImage;

			if (!_imageDictionary.TryGetValue(sIconName, out iconImage))
			{
				// Not in dictionary yet.  Load, scale, and cache
				iconImage = GetIconImageFromFileAndScale(sIconName);

				// cache result of load and scale to reduce processing
				// next time the image is needed
				_imageDictionary.Add(sIconName, iconImage);
			}

			return iconImage; // this could be null
		}


		/// <summary>
		/// Gets the icon image from file and scale.
		/// </summary>
		/// <returns>The icon image from file and scale.</returns>
		/// <param name="sIconName">S icon name.</param>
		UIImage GetIconImageFromFileAndScale(string sIconName)
		{
			UIImage iconImage = UIImage.FromBundle(sIconName);

			// If no corresponding icon found on device, use a default
			if (iconImage == null)
			{
				if (sIconName.StartsWith("BITS", StringComparison.InvariantCultureIgnoreCase))
					iconImage = UIImage.FromBundle("BITS.png");
				else
					iconImage = UIImage.FromBundle("ATOM.png");
			}

			iconImage = GetScaledImage(iconImage);

			return iconImage;
		}


		/// <summary>
		/// Gets the scaled image while maintaining correct aspect ration and not blurring.
		/// 
		/// See iOS code version at https://gist.github.com/tomasbasham/10533743
		/// </summary>
		/// <returns>The scaled image.</returns>
		/// <param name="originalImage">Original image.</param>
		UIImage GetScaledImage(UIImage originalImage)
		{
			CGSize newSize = new CGSize(originalImage.Size.Width / SCALING_FACTOR, originalImage.Size.Height / SCALING_FACTOR);

			CGRect scaledImageRect = CGRect.Empty;

			double aspectWidth = newSize.Width / originalImage.Size.Width;
			double aspectHeight = newSize.Height / originalImage.Size.Height;
			double aspectRatio = Math.Min(aspectWidth, aspectHeight);

			scaledImageRect.Size = new CGSize(originalImage.Size.Width * aspectRatio, originalImage.Size.Height * aspectRatio);
			scaledImageRect.X = (newSize.Width - scaledImageRect.Size.Width) / 2.0f;
			scaledImageRect.Y = (newSize.Height - scaledImageRect.Size.Height) / 2.0f;

			UIGraphics.BeginImageContextWithOptions(newSize, false, 0);
			originalImage.Draw(scaledImageRect);

			UIImage scaledImage = UIGraphics.GetImageFromCurrentImageContext();
			UIGraphics.EndImageContext();
			return scaledImage;
		}
	}
}
