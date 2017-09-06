using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class GitPathView : Subview
    {
        private const string BrowseButton = "...";
        private const string GitInstallTitle = "Git installation";
        private const string GitInstallBrowseTitle = "Select git binary";
        private const string GitInstallPickInvalidTitle = "Invalid Git install";
        private const string GitInstallPickInvalidMessage = "The selected file is not a valid Git install. {0}";
        private const string GitInstallFindButton = "Find install";
        private const string GitInstallPickInvalidOK = "OK";
        private const string PathToGit = "Path to Git";
        private const string GitPathSaveButton = "Save Path";

        [SerializeField] private string gitExec;
        [SerializeField] private string gitExecParent;
        [SerializeField] private string gitExecExtension;
        [SerializeField] private string newGitExec;
        [NonSerialized] private bool gitExecHasChanged;

        [NonSerialized] private bool isBusy;
        public override void OnEnable()
        {
            base.OnEnable();

            gitExecHasChanged = true;
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();
            MaybeUpdateData();
        }

        public override bool IsBusy
        {
            get { return isBusy; }
        }

        public override void OnGUI()
        {
            // Install path
            GUILayout.Label(GitInstallTitle, EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(IsBusy || Parent.IsBusy);
            {
                // Install path field
                GUILayout.BeginHorizontal();
                {
                    newGitExec = EditorGUILayout.TextField(PathToGit, newGitExec);

                    if (GUILayout.Button(BrowseButton, EditorStyles.miniButton, GUILayout.Width(25)))
                    {
                        GUI.FocusControl(null);

                        var newValue = EditorUtility.OpenFilePanel(GitInstallBrowseTitle,
                            gitExecParent,
                            gitExecExtension);

                        if (!string.IsNullOrEmpty(newValue))
                        {
                            newGitExec = newValue;
                        }
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                GUILayout.BeginHorizontal();
                {
                    var needsSaving = !string.IsNullOrEmpty(newGitExec)
                        && newGitExec != gitExec
                        && newGitExec.ToNPath().FileExists();

                    EditorGUI.BeginDisabledGroup(!needsSaving);
                    {
                        if (GUILayout.Button(GitPathSaveButton, GUILayout.ExpandWidth(false)))
                        {
                            Logger.Trace("Saving Git Path:{0}", newGitExec);

                            GUI.FocusControl(null);

                            Manager.SystemSettings.Set(Constants.GitInstallPathKey, newGitExec);
                            Environment.GitExecutablePath = newGitExec.ToNPath();
                            gitExecHasChanged = true;
                        }
                    }
                    EditorGUI.EndDisabledGroup();

                    //Find button - for attempting to locate a new install
                    if (GUILayout.Button(GitInstallFindButton, GUILayout.ExpandWidth(false)))
                    {
                        GUI.FocusControl(null);

                        var task = new ProcessTask<NPath>(Manager.CancellationToken, new FirstLineIsPathOutputProcessor())
                            .Configure(Manager.ProcessManager, Environment.IsWindows ? "where" : "which", "git")
                            .FinallyInUI((success, ex, path) =>
                            {
                                Logger.Trace("Find Git Completed Success:{0} Path:{1}", success, path);

                                if (success && !string.IsNullOrEmpty(path))
                                {
                                    Manager.SystemSettings.Set(Constants.GitInstallPathKey, path);
                                    Environment.GitExecutablePath = path;
                                    gitExecHasChanged = true;
                                }
                            });
                    }
                }
                GUILayout.EndHorizontal();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void MaybeUpdateData()
        {
            if (gitExecHasChanged)
            {
                if (Environment != null)
                {
                    if (gitExecExtension == null)
                    {
                        gitExecExtension = Environment.ExecutableExtension;

                        if (Environment.IsWindows)
                        {
                            gitExecExtension = gitExecExtension.TrimStart('.');
                        }
                    }

                    if (Environment.GitExecutablePath != null)
                    {
                        newGitExec = gitExec = Environment.GitExecutablePath.ToString();
                        gitExecParent = Environment.GitExecutablePath.Parent.ToString();
                    }

                    if (gitExecParent == null)
                    {
                        gitExecParent = Environment.GitInstallPath;
                    }
                }

                gitExecHasChanged = false;
            }
        }
    }
}