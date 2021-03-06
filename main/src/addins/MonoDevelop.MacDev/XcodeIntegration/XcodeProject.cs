// 
// XcodeProject.cs
//  
// Authors:
//       Geoff Norton <gnorton@novell.com>
//       Jeffrey Stedfast <jeff@xamarin.com>
// 
// Copyright (c) 2011 Novell, Inc.
// COpyright (c) 2011 Xamarin Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;


namespace MonoDevelop.MacDev.XcodeIntegration
{
	public class XcodeProject
	{
		string name;
		PBXProject project;
		List<PBXFileReference> files;
		List<PBXBuildFile> sources;
		PBXGroup frameworksGroup;
		PBXGroup productsGroup;
		PBXGroup projectGroup;
		PBXGroup mainGroup;
		List<PBXGroup> groups;
		PBXFileReference target;
		PBXNativeTarget nativeTarget;
		XCConfigurationList nativeConfigurationList;
		XCConfigurationList projectConfigurationList;
		XCBuildConfiguration nativeBuildConfiguration;
		XCBuildConfiguration projectBuildConfiguration;
		PBXFrameworksBuildPhase frameworksBuildPhase;
		PBXResourcesBuildPhase resourcesBuildPhase;
		PBXSourcesBuildPhase sourcesBuildPhase;

		public PBXGroup ProjectGroup {
			get {
				return projectGroup;
			}
		}
		
		public XcodeProject (string name, string sdkRoot, string configName)
		{
			this.name = name;

			frameworksGroup = new PBXGroup ("Frameworks", XcodeObjectSortDirection.Descending);
			mainGroup = new PBXGroup (null, XcodeObjectSortDirection.None);
			productsGroup = new PBXGroup ("Products");
			projectGroup = new PBXGroup (name);
			
			this.frameworksBuildPhase = new PBXFrameworksBuildPhase ();
			this.resourcesBuildPhase = new PBXResourcesBuildPhase ();
			this.sourcesBuildPhase = new PBXSourcesBuildPhase ();
			this.files = new List<PBXFileReference> ();
			this.sources = new List<PBXBuildFile> ();
			this.groups = new List<PBXGroup> ();
			
			this.groups.Add (mainGroup);
			this.groups.Add (productsGroup);
			this.groups.Add (frameworksGroup);
			this.groups.Add (projectGroup);

			mainGroup.AddChild (projectGroup);
			mainGroup.AddChild (frameworksGroup);
			mainGroup.AddChild (productsGroup);
			
			this.target = new PBXFileReference (string.Format ("{0}.app", name), "BUILT_PRODUCTS_DIR");
			productsGroup.AddChild (this.target);
			
			this.nativeConfigurationList = new XCConfigurationList ();
			this.projectConfigurationList = new XCConfigurationList ();
			this.nativeBuildConfiguration = new XCBuildConfiguration (configName);
			this.projectBuildConfiguration = new XCBuildConfiguration (configName);
			this.nativeTarget = new PBXNativeTarget (name, nativeConfigurationList, target);
			this.project = new PBXProject (name, projectConfigurationList, mainGroup, productsGroup);

			projectBuildConfiguration.AddSetting ("ALWAYS_SEARCH_USER_PATHS", "NO");
			projectBuildConfiguration.AddSetting ("ARCHS", "\"$(ARCHS_STANDARD_32_BIT)\"");
			//projectBuildConfiguration.AddSetting ("\"CODE_SIGN_IDENTITY[sdk=" + sdkRoot + "*]\"", "\"IPhone Developer\"");
			projectBuildConfiguration.AddSetting ("COPY_PHASE_STRIP", "NO");
			projectBuildConfiguration.AddSetting ("GCC_C_LANGUAGE_STANDARD", "gnu99");
			projectBuildConfiguration.AddSetting ("GCC_DYNAMIC_NO_PIC", "NO");
			projectBuildConfiguration.AddSetting ("GCC_OPTIMIZATION_LEVEL", "0");
			//projectBuildConfiguration.AddSetting ("GCC_PREPROCESSOR_DEFINITIONS", "(\"DEBUG=1\", \"$(inherited)\", )");
			projectBuildConfiguration.AddSetting ("GCC_SYMBOLS_PRIVATE_EXTERN", "NO");
			projectBuildConfiguration.AddSetting ("GCC_VERSION", "com.apple.compilers.llvm.clang.1_0");
			projectBuildConfiguration.AddSetting ("GCC_WARN_ABOUT_MISSING_PROTOTYPES", "YES");
			projectBuildConfiguration.AddSetting ("GCC_WARN_ABOUT_RETURN_TYPE", "YES");
			projectBuildConfiguration.AddSetting ("GCC_WARN_UNUSED_VARIABLE", "YES");
			projectBuildConfiguration.AddSetting ("OTHER_CFLAGS", "\"\"");
			projectBuildConfiguration.AddSetting ("OTHER_LDFLAGS", "\"\"");
			projectBuildConfiguration.AddSetting ("SDKROOT", sdkRoot);

			this.projectConfigurationList.AddBuildConfiguration (projectBuildConfiguration);

			nativeBuildConfiguration.AddSetting ("GCC_PRECOMPILE_PREFIX_HEADER", "NO");
			//nativeBuildConfiguration.AddSetting ("INFOPLIST_FILE", "\"Info.plist\"");
			nativeBuildConfiguration.AddSetting ("PRODUCT_NAME", name);
			nativeBuildConfiguration.AddSetting ("WRAPPER_EXTENSION", "app");

			this.nativeConfigurationList.AddBuildConfiguration (nativeBuildConfiguration);

			this.nativeTarget.AddBuildPhase (sourcesBuildPhase);
			this.nativeTarget.AddBuildPhase (frameworksBuildPhase);
			this.nativeTarget.AddBuildPhase (resourcesBuildPhase);

			this.files.Add (target);
			this.project.AddNativeTarget (nativeTarget);
		}
		
		public string Name { get { return name; } }

		PBXBuildFile AddFile (string path, string tree, PBXGroup grp = null)
		{
			var fileref = new PBXFileReference (path, tree);
			var buildfile = new PBXBuildFile (fileref);

			files.Add (fileref);
			sources.Add (buildfile);
			(grp ?? projectGroup).AddChild (fileref);

			return buildfile;
		}

		public void AddResource (string path, PBXGroup grp = null)
		{
			resourcesBuildPhase.AddResource (AddFile (path, "\"<group>\"", grp));
		}

		public void AddPlist (string name)
		{
			var fileref = new PBXFileReference (name, "\"<group>\"");
			files.Add (fileref);
		}

		public void AddSource (string name, PBXGroup grp = null)
		{
			//sourcesBuildPhase.AddSource (AddFile (Path.GetFileName (name), Path.GetDirectoryName (name), "\"<group>\""));
			sourcesBuildPhase.AddSource (AddFile (name, "\"<group>\"", grp));
		}
		
		public PBXGroup AddGroup (PBXGroup parent, string name)
		{
			var result = new PBXGroup (name);
			parent.AddChild (result);
			groups.Add (result);
			return result;
		}
		
		public PBXGroup AddGroup (string name)
		{
			return AddGroup (projectGroup, name);
		}
		
		public PBXGroup GetGroup (string name)
		{
			return groups.FirstOrDefault (g => g.Name == name);
		}
		
		public PBXGroup GetGroup (PBXGroup parent, string name)
		{
			foreach (var obj in parent) {
				var grp = obj as PBXGroup;
				if (grp != null && grp.Name ==name)
					return grp;
			}
			return null;
		}
		
		public void AddResourceDirectory (string directory)
		{
			var fileref = new PBXFileReference (directory, "\"<group>\"");
			files.Add (fileref);
		}

		public void AddFramework (string framework)
		{
			frameworksBuildPhase.AddFramework (AddFile (string.Format ("System/Library/Frameworks/{0}.framework", framework), "SDKROOT", frameworksGroup));
		}

		public void Generate (string outputPath)
		{
			var dir = Path.Combine (outputPath, string.Format ("{0}.xcodeproj", name));

			if (!Directory.Exists (dir))
				Directory.CreateDirectory (dir);
			
			File.WriteAllText (Path.Combine (dir, "project.pbxproj"), this.ToString ());

			dir = Path.Combine (dir, "project.xcworkspace");
			if (!Directory.Exists (dir))
				Directory.CreateDirectory (dir);

			File.WriteAllText (Path.Combine (dir, "contents.xcworkspacedata"), string.Format (
					   "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
					   "<Workspace\n" +
					   "   version = \"1.0\">\n" +
					   "  <FileRef\n" +
					   "     location = \"self:{0}.xcodeproj\">\n" +
					   "  </FileRef>\n" +
					   "</Workspace>\n", name));
		}

		public override string ToString ()
		{
			var sb = new StringBuilder ();

			sb.Append ("// !$*UTF8*$!\n");
			sb.Append ("{\n");
			sb.Append ("\tarchiveVersion = 1;\n");
			sb.Append ("\tclasses = {\n");
			sb.Append ("\t};\n");
			sb.Append ("\tobjectVersion = 46;\n");
			sb.Append ("\tobjects = {\n");
			sb.Append ("\n");
			sb.Append ("/* Begin PBXBuildFile section */\n");
			foreach (PBXBuildFile pbxbf in sources)
				sb.AppendFormat ("\t\t{0}\n", pbxbf);
			sb.Append ("/* End PBXBuildFile section */\n\n");

			sb.Append ("/* Begin PBXFileReference section */\n");
			foreach (PBXFileReference pbxfr in files)
				sb.AppendFormat ("\t\t{0}\n", pbxfr);
			sb.Append ("/* End PBXFileReference section */\n\n");

			sb.Append ("/* Begin PBXFrameworksBuildPhase section */\n");
			sb.AppendFormat ("\t\t{0}\n", frameworksBuildPhase);
			sb.Append ("/* End PBXFrameworksBuildPhase section */\n\n");

			sb.Append ("/* Begin PBXGroup section */\n");
			foreach (var grp in groups)
				sb.AppendFormat ("\t\t{0}\n", grp);
			sb.Append ("/* End PBXGroup section */\n\n");

			sb.Append ("/* Begin PBXNativeTarget section */\n");
			sb.AppendFormat ("\t\t{0}\n", nativeTarget);
			sb.Append ("/* End PBXNativeTarget section */\n\n");

			sb.Append ("/* Begin PBXProject section */\n");
			sb.AppendFormat ("\t\t{0}\n", project);
			sb.Append ("/* End PBXProject section */\n\n");

			sb.Append ("/* Begin PBXResourcesBuildPhase section */\n");
			sb.AppendFormat ("\t\t{0}\n", resourcesBuildPhase);
			sb.Append ("/* End PBXResourcesBuildPhase section */\n\n");

			sb.Append ("/* Begin PBXSourcesBuildPhase section */\n");
			sb.AppendFormat ("\t\t{0}\n", sourcesBuildPhase);
			sb.Append ("/* End PBXSourcesBuildPhase section */\n\n");

			sb.Append ("/* Begin PBXVariantGroup section */\n");
			// FIXME: add a PBXVariantGroup?
			sb.Append ("/* End PBXVariantGroup section */\n\n");

			sb.Append ("/* Begin XCBuildConfiguration section */\n");
			sb.AppendFormat ("\t\t{0}\n", projectBuildConfiguration);
			sb.AppendFormat ("\t\t{0}\n", nativeBuildConfiguration);
			sb.Append ("/* End XCBuildConfiguration section */\n\n");

			sb.Append ("/* Begin XCConfigurationList section */\n");
			sb.AppendFormat ("\t\t{0}\n", projectConfigurationList);
			sb.AppendFormat ("\t\t{0}\n", nativeConfigurationList);
			sb.Append ("/* End XCConfigurationList section */\n");

			sb.Append ("\t};\n");
			sb.AppendFormat ("\trootObject = {0} /* Project object */;\n", project.Token);
			sb.Append ("}");

			return sb.ToString ();
		}
	}
}
