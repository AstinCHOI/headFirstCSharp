using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Save_the_Humans.Common
{
    /// <summary>
    /// SuspensionManager는 전역 세션 상태를 capture하여 응용 프로그램에 대한 프로세스
    /// 수명 관리를 간단하게 합니다. 세션 상태는 다양한 조건에 따라 자동으로
    /// 지워지므로 세션 간에 이동하기 쉽지만 응용 프로그램이 충돌하거나
    /// 업그레이드될 때 삭제되어야 하는 정보를 저장하는 데에만 사용되어야
    /// 합니다.
    /// </summary>
    internal sealed class SuspensionManager
    {
        private static Dictionary<string, object> _sessionState = new Dictionary<string, object>();
        private static List<Type> _knownTypes = new List<Type>();
        private const string sessionStateFilename = "_sessionState.xml";

        /// <summary>
        /// 현재 세션의 전역 세션 상태에 대한 액세스를 제공합니다. 이 상태는
        /// <see cref="SaveAsync"/>로 serialize되고 <see cref="RestoreAsync"/>로
        /// 복원되므로 값은 <see cref="DataContractSerializer"/>로 serialize할
        /// 수 있고 가능한 한 간단해야 합니다. 문자열 및 기타 자체 포함 데이터 형식을
        /// 사용하는 것이 좋습니다.
        /// </summary>
        public static Dictionary<string, object> SessionState
        {
            get { return _sessionState; }
        }

        /// <summary>
        /// 세션 상태를 읽고 쓸 때 <see cref="DataContractSerializer"/>에 제공되는
        /// 사용자 지정 형식 목록입니다. 처음에는 비어 있고 serialization 프로세스를
        /// 사용자 지정하기 위해 추가 형식을 추가할 수도 있습니다.
        /// </summary>
        public static List<Type> KnownTypes
        {
            get { return _knownTypes; }
        }

        /// <summary>
        /// 현재 <see cref="SessionState"/>를 저장합니다. <see cref="RegisterFrame"/>에 등록된
        /// 모든 <see cref="Frame"/> 인스턴스는 현재 탐색 스택도 유지합니다.
        /// 따라서 활성 <see cref="Page"/>에 해당 상태를 저장할 수 있는 기회를
        /// 제공합니다.
        /// </summary>
        /// <returns>세션 상태가 저장된 시기를 반영하는 비동기 작업입니다.</returns>
        public static async Task SaveAsync()
        {
            try
            {
                // 등록된 모든 프레임에 대한 탐색 상태를 저장합니다.
                foreach (var weakFrameReference in _registeredFrames)
                {
                    Frame frame;
                    if (weakFrameReference.TryGetTarget(out frame))
                    {
                        SaveFrameNavigationState(frame);
                    }
                }

                // 공유 상태에 대해 비동기적으로 액세스하지 못하도록 세션 상태를 동기적으로
                // serialize합니다.
                MemoryStream sessionData = new MemoryStream();
                DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, object>), _knownTypes);
                serializer.WriteObject(sessionData, _sessionState);

                // SessionState 파일에 대한 출력 스트림을 가져와 상태를 비동기적으로 씁니다.
                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(sessionStateFilename, CreationCollisionOption.ReplaceExisting);
                using (Stream fileStream = await file.OpenStreamForWriteAsync())
                {
                    sessionData.Seek(0, SeekOrigin.Begin);
                    await sessionData.CopyToAsync(fileStream);
                }
            }
            catch (Exception e)
            {
                throw new SuspensionManagerException(e);
            }
        }

        /// <summary>
        /// 이전에 저장된 <see cref="SessionState"/>를 복원합니다. <see cref="RegisterFrame"/>으로 등록된 모든
        /// <see cref="Frame"/> 인스턴스도 이전의 탐색 상태를 복원합니다.
        /// 따라서 활성 <see cref="Page"/>에 해당 상태를 복원할 수 있는 기회를
        /// 제공합니다.
        /// </summary>
        /// <param name="sessionBaseKey">세션의 형식을 식별하는 선택 키입니다.
        /// 이것은 여러 응용 프로그램 시작 시나리오의 구분에 사용할 수 있습니다.</param>
        /// <returns>세션 상태를 읽은 시기를 반영하는 비동기 작업입니다.
        /// <see cref="SessionState"/>의 내용은 이 작업이 완료될 때까지 신뢰해서는
        /// 안 됩니다.</returns>
        public static async Task RestoreAsync(String sessionBaseKey = null)
        {
            _sessionState = new Dictionary<String, Object>();

            try
            {
                // SessionState 파일에 대한 입력 스트림을 가져옵니다.
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(sessionStateFilename);
                using (IInputStream inStream = await file.OpenSequentialReadAsync())
                {
                    // 세션 상태를 deserialize합니다.
                    DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, object>), _knownTypes);
                    _sessionState = (Dictionary<string, object>)serializer.ReadObject(inStream.AsStreamForRead());
                }

                // 등록된 모든 프레임을 저장된 상태로 복원합니다.
                foreach (var weakFrameReference in _registeredFrames)
                {
                    Frame frame;
                    if (weakFrameReference.TryGetTarget(out frame) && (string)frame.GetValue(FrameSessionBaseKeyProperty) == sessionBaseKey)
                    {
                        frame.ClearValue(FrameSessionStateProperty);
                        RestoreFrameNavigationState(frame);
                    }
                }
            }
            catch (Exception e)
            {
                throw new SuspensionManagerException(e);
            }
        }

        private static DependencyProperty FrameSessionStateKeyProperty =
            DependencyProperty.RegisterAttached("_FrameSessionStateKey", typeof(String), typeof(SuspensionManager), null);
        private static DependencyProperty FrameSessionBaseKeyProperty =
            DependencyProperty.RegisterAttached("_FrameSessionBaseKeyParams", typeof(String), typeof(SuspensionManager), null);
        private static DependencyProperty FrameSessionStateProperty =
            DependencyProperty.RegisterAttached("_FrameSessionState", typeof(Dictionary<String, Object>), typeof(SuspensionManager), null);
        private static List<WeakReference<Frame>> _registeredFrames = new List<WeakReference<Frame>>();

        /// <summary>
        /// <see cref="Frame"/> 인스턴스를 등록하면 해당 탐색 기록을
        /// <see cref="SessionState"/>에 저장하고 여기서 복원합니다. 프레임이 세션 상태 관리에
        /// 참여할 경우 프레임은 작성 후 바로 등록해야 합니다.
        /// 상태가 지정된 키에 대해 이미 복원된 경우 등록하면
        /// 탐색 기록은 바로 복원됩니다. <see cref="RestoreAsync(String)"/>의
        /// <see cref="RestoreAsync"/> 후속 호출도 탐색 기록을 복원합니다.
        /// </summary>
        /// <param name="frame">탐색 기록이 <see cref="SuspensionManager"/>에서 관리되어야 하는
        /// <see cref="SuspensionManager"/></param>
        /// <param name="sessionStateKey">탐색 관련 정보를 저장하는 데 사용되는 <see cref="SessionState"/>의
        /// 고유한 키입니다.</param>
        /// <param name="sessionBaseKey">세션의 형식을 식별하는 선택 키입니다.
        /// 이것은 여러 응용 프로그램 시작 시나리오의 구분에 사용할 수 있습니다.</param>
        public static void RegisterFrame(Frame frame, String sessionStateKey, String sessionBaseKey = null)
        {
            if (frame.GetValue(FrameSessionStateKeyProperty) != null)
            {
                throw new InvalidOperationException("Frames can only be registered to one session state key");
            }

            if (frame.GetValue(FrameSessionStateProperty) != null)
            {
                throw new InvalidOperationException("Frames must be either be registered before accessing frame session state, or not registered at all");
            }

            if (!string.IsNullOrEmpty(sessionBaseKey))
            {
                frame.SetValue(FrameSessionBaseKeyProperty, sessionBaseKey);
                sessionStateKey = sessionBaseKey + "_" + sessionStateKey;
            }

            // 종속성 속성을 사용하여 세션 키를 프레임에 연결하고, 탐색 상태를 관리해야 하는
            // 프레임 목록을 유지합니다.
            frame.SetValue(FrameSessionStateKeyProperty, sessionStateKey);
            _registeredFrames.Add(new WeakReference<Frame>(frame));

            // 탐색 상태를 복원할 수 있는지 확인하십시오.
            RestoreFrameNavigationState(frame);
        }

        /// <summary>
        /// <see cref="RegisterFrame"/>으로 <see cref="SessionState"/>에서 이전에 등록된
        /// <see cref="Frame"/>의 연결을 끊습니다. 이전에 캡처된 모두 탐색 상태는
        /// 제거됩니다.
        /// </summary>
        /// <param name="frame">탐색 기록이 더 이상 관리되지 않는
        ///인스턴스입니다.</param>
        public static void UnregisterFrame(Frame frame)
        {
            // 세션 상태를 제거하고 탐색 상태가 더 이상 접근할 수 없는 약한 참조와 함께 저장되는
            // 프레임 목록에서 프레임을 제거합니다.
            SessionState.Remove((String)frame.GetValue(FrameSessionStateKeyProperty));
            _registeredFrames.RemoveAll((weakFrameReference) =>
            {
                Frame testFrame;
                return !weakFrameReference.TryGetTarget(out testFrame) || testFrame == frame;
            });
        }

        /// <summary>
        /// 지정된 <see cref="Frame"/>에 연결된 세션 상태에 대한 저장소를 제공합니다.
        /// <see cref="RegisterFrame"/>으로 이미 등록된 프레임에는
        /// 세션 상태가 <see cref="SessionState"/>의 일부로 자동으로 저장 및
        /// 복원됩니다. 등록되지 않은 프레임에는 탐색 캐시에서
        /// 삭제된 페이지를 복원할 때 유용할 수 있는 임시 상태가
        /// 있습니다.
        /// </summary>
        /// <remarks>앱은 <see cref="NavigationHelper"/>를 사용하도록 선택하여
        /// 프레임 세션 상태로 직접 작업하는 대신 페이지별 상태를 관리할 수도 있습니다.</remarks>
        /// <param name="frame">세션 상태가 필요한 인스턴스입니다.</param>
        /// <returns><see cref="SessionState"/>와 동일한 serialization 메커니즘을 따르는 상태
        /// 컬렉션입니다.</returns>
        public static Dictionary<String, Object> SessionStateForFrame(Frame frame)
        {
            var frameState = (Dictionary<String, Object>)frame.GetValue(FrameSessionStateProperty);

            if (frameState == null)
            {
                var frameSessionKey = (String)frame.GetValue(FrameSessionStateKeyProperty);
                if (frameSessionKey != null)
                {
                    // 등록된 프레임은 해당 세션 상태를 반영합니다.
                    if (!_sessionState.ContainsKey(frameSessionKey))
                    {
                        _sessionState[frameSessionKey] = new Dictionary<String, Object>();
                    }
                    frameState = (Dictionary<String, Object>)_sessionState[frameSessionKey];
                }
                else
                {
                    // 등록되지 않은 프레임에는 임시 상태가 있습니다.
                    frameState = new Dictionary<String, Object>();
                }
                frame.SetValue(FrameSessionStateProperty, frameState);
            }
            return frameState;
        }

        private static void RestoreFrameNavigationState(Frame frame)
        {
            var frameState = SessionStateForFrame(frame);
            if (frameState.ContainsKey("Navigation"))
            {
                frame.SetNavigationState((String)frameState["Navigation"]);
            }
        }

        private static void SaveFrameNavigationState(Frame frame)
        {
            var frameState = SessionStateForFrame(frame);
            frameState["Navigation"] = frame.GetNavigationState();
        }
    }
    public class SuspensionManagerException : Exception
    {
        public SuspensionManagerException()
        {
        }

        public SuspensionManagerException(Exception e)
            : base("SuspensionManager failed", e)
        {

        }
    }
}
