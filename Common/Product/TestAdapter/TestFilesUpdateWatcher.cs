﻿//*********************************************************//
//    Copyright (c) Microsoft. All rights reserved.
//    
//    Apache 2.0 License
//    
//    You may obtain a copy of the License at
//    http://www.apache.org/licenses/LICENSE-2.0
//    
//    Unless required by applicable law or agreed to in writing, software 
//    distributed under the License is distributed on an "AS IS" BASIS, 
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or 
//    implied. See the License for the specific language governing 
//    permissions and limitations under the License.
//
//*********************************************************//

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudioTools;

namespace Microsoft.VisualStudioTools.TestAdapter {
    class TestFilesUpdateWatcher : IDisposable {
        private readonly IDictionary<string, FileSystemWatcher> _fileWatchers;
        public event EventHandler<TestFileChangedEventArgs> FileChangedEvent;

        public TestFilesUpdateWatcher() {
            _fileWatchers = new Dictionary<string, FileSystemWatcher>(StringComparer.OrdinalIgnoreCase);
        }

        public bool AddWatch(string path) {
            ValidateArg.NotNull(path,"path");

            if (!String.IsNullOrEmpty(path)) {
                var directoryName = Path.GetDirectoryName(path);
                var filter = Path.GetFileName(path);

                if (!_fileWatchers.ContainsKey(path) && Directory.Exists(directoryName)) {
                    var watcher = new FileSystemWatcher(directoryName, filter);
                    _fileWatchers[path] = watcher;

                    watcher.Changed += OnChanged;
                    watcher.EnableRaisingEvents = true;
                    return true;
                }
            }
            return false;
        }

        public bool AddDirectoryWatch(string path) {
            ValidateArg.NotNull(path, "path");

            if (!String.IsNullOrEmpty(path)) {
                if (!_fileWatchers.ContainsKey(path) && Directory.Exists(path)) {
                    var watcher = new FileSystemWatcher(path);
                    _fileWatchers[path] = watcher;

                    watcher.IncludeSubdirectories = true;
                    watcher.Changed += OnChanged;
                    watcher.Renamed += OnRenamed;
                    watcher.EnableRaisingEvents = true;
                    return true;
                }
            }
            return false;
        }

        public void RemoveWatch(string path) {
            ValidateArg.NotNull(path, "path");

            if (!String.IsNullOrEmpty(path)) {
                FileSystemWatcher watcher;

                if (_fileWatchers.TryGetValue(path, out watcher)) {
                    watcher.EnableRaisingEvents = false;

                    _fileWatchers.Remove(path);

                    watcher.Changed -= OnChanged;
                    watcher.Dispose();
                }
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e) {
            var evt = FileChangedEvent;
            if (evt != null) {
                evt(sender, new TestFileChangedEventArgs(null, e.FullPath, TestFileChangedReason.Changed));
            }
        }

        private void OnRenamed(object sender, RenamedEventArgs e) {
            var evt = FileChangedEvent;
            if (evt != null) {
                evt(sender, new TestFileChangedEventArgs(null, e.FullPath, TestFileChangedReason.Renamed));
            }
        }

        public void Dispose() {
            Dispose(true);
            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing && _fileWatchers != null) {
                foreach (var watcher in _fileWatchers.Values) {
                    if (watcher != null) {
                        watcher.Changed -= OnChanged;
                        watcher.Dispose();
                    }
                }

                _fileWatchers.Clear();
            }
        }
    }
}
