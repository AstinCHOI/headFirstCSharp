using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AnimatedBee.Common
{
    /// <summary>
    /// 대리자를 호출하여 다른 개체에 기능을 릴레이하는 것이 
    /// 목적인 명령입니다. 
    /// CanExecute 메서드의 기본 반환 값은 'true'입니다.
    /// <see cref="RaiseCanExecuteChanged"/> 호출이 필요합니다.
    /// <see cref="CanExecute"/>가 다른 값으로 반환할 때마다
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>
        /// RaiseCanExecuteChanged이 호출될 때 발생합니다.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// 항상 실행할 수 있는 새 명령을 만듭니다.
        /// </summary>
        /// <param name="execute">실행 논리입니다.</param>
        public RelayCommand(Action execute)
            : this(execute, null)
        {
        }

        /// <summary>
        /// 새 명령을 만듭니다.
        /// </summary>
        /// <param name="execute">실행 논리입니다.</param>
        /// <param name="canExecute">실행 상태 논리입니다.</param>
        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");
            _execute = execute;
            _canExecute = canExecute;
        }

        /// <summary>
        /// 현재 상태에서 <see cref="RelayCommand"/>를 실행할 수 있는지 여부를 확인합니다.
        /// </summary>
        /// <param name="parameter">
        /// 명령으로 사용된 데이터입니다. 명령에 전달되어야 할 데이터가 필요하지 않으면 이 개체는 null로 설정됩니다.
        /// </param>
        /// <returns>이 명령이 실행되면 true를 그렇지 않으면 false를 반환합니다.</returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute();
        }

        /// <summary>
        /// 현재 명령 대상에서 <see cref="RelayCommand"/>를 실행합니다.
        /// </summary>
        /// <param name="parameter">
        /// 명령으로 사용된 데이터입니다. 명령에 전달되어야 할 데이터가 필요하지 않으면 이 개체는 null로 설정됩니다.
        /// </param>
        public void Execute(object parameter)
        {
            _execute();
        }

        /// <summary>
        /// <see cref="CanExecuteChanged"/> 이벤트를 발생시키기 위해 사용된 메서드입니다.
        /// <see cref="CanExecute"/> 메서드의 반환 값이
        /// 변경된 것을 나타냅니다.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            var handler = CanExecuteChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}