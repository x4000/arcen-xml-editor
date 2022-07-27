using System;

namespace ArcenXE.Utilities.MetadataProcessing.BooleanLogic
{
    public class BooleanLogicCheckerTree
    {
        public readonly string Name;
        public readonly LogicGroup RootGroup;

        public BooleanLogicCheckerTree( string Name, LogicGroup RootGroup )
        {
            this.Name = Name;
            this.RootGroup = RootGroup;
        }

        public bool GetDoesPass()
        {
            return this.RootGroup.GetDoesPass();
        }

        #region LogicGroup
        public abstract class LogicGroup
        {
            public readonly List<ISingleChecker> DirectCheckers = new List<ISingleChecker>();
            public readonly List<LogicGroup> SubGroups = new List<LogicGroup>();

            public abstract bool GetDoesPass();
        }
        #endregion

        #region AndGroup
        public class AndGroup : LogicGroup
        {
            public override bool GetDoesPass()
            {
                foreach ( ISingleChecker checker in this.DirectCheckers )
                    if ( !checker.GetDoesPass() )
                        return false;

                foreach ( LogicGroup group in this.SubGroups )
                    if ( !group.GetDoesPass() )
                        return false;

                //only true if all of them return true
                return true;
            }
        }
        #endregion AndGroup

        #region OrGroup
        public class OrGroup : LogicGroup
        {
            public override bool GetDoesPass()
            {
                foreach ( ISingleChecker checker in this.DirectCheckers )
                    if ( checker.GetDoesPass() )
                        return true;

                foreach ( LogicGroup group in this.SubGroups )
                    if ( group.GetDoesPass() )
                        return true;

                //only false if none of them are true
                return false;
            }
        }
        #endregion OrGroup

        public interface ISingleChecker
        {
            bool GetDoesPass();
        }

        #region StringChecker
        public class StringChecker : ISingleChecker
        {
            public readonly string TargetValue;
            public readonly BooleanLogicType LogicType;
            public readonly GetString StringGetter;

            public StringChecker( string TargetValue, BooleanLogicType LogicType, GetString StringGetter )
            {
                this.TargetValue = TargetValue;
                this.LogicType = LogicType;
                this.StringGetter = StringGetter;
            }

            public bool GetDoesPass()
            {
                string str = StringGetter();
                switch ( this.LogicType )
                {
                    case BooleanLogicType.Equals:
                        return str == TargetValue;
                    case BooleanLogicType.NotEqual:
                        return str != TargetValue;
                    default:
                        throw new Exception( "BooleanLogicType " + this.LogicType + " is not valid for a StringChecker!" );
                }
            }

            public delegate string GetString();
        }
        #endregion StringChecker

        #region IntChecker
        public class IntChecker : ISingleChecker
        {
            public readonly int TargetValue;
            public readonly BooleanLogicType LogicType;
            public readonly GetInt IntGetter;

            public IntChecker( int TargetValue, BooleanLogicType LogicType, GetInt IntGetter )
            {
                this.TargetValue = TargetValue;
                this.LogicType = LogicType;
                this.IntGetter = IntGetter;
            }

            public bool GetDoesPass()
            {
                int intVal = IntGetter();
                switch ( this.LogicType )
                {
                    case BooleanLogicType.Equals:
                        return intVal == TargetValue;
                    case BooleanLogicType.NotEqual:
                        return intVal != TargetValue;
                    case BooleanLogicType.LessThan:
                        return intVal < TargetValue;
                    case BooleanLogicType.LessThanOrEqualTo:
                        return intVal <= TargetValue;
                    case BooleanLogicType.GreaterThan:
                        return intVal > TargetValue;
                    case BooleanLogicType.GreaterThanOrEqualTo:
                        return intVal >= TargetValue;
                    default:
                        throw new Exception( "BooleanLogicType " + this.LogicType + " is not valid for a IntChecker!" );
                }
            }

            public delegate int GetInt();
        }
        #endregion IntChecker

        #region FloatChecker
        public class FloatChecker : ISingleChecker
        {
            public readonly float TargetValue;
            public readonly BooleanLogicType LogicType;
            public readonly GetFloat FloatGetter;

            public FloatChecker( float TargetValue, BooleanLogicType LogicType, GetFloat FloatGetter )
            {
                this.TargetValue = TargetValue;
                this.LogicType = LogicType;
                this.FloatGetter = FloatGetter;
            }

            public bool GetDoesPass()
            {
                float floatVal = FloatGetter();
                switch ( this.LogicType )
                {
                    case BooleanLogicType.Equals:
                        return floatVal == TargetValue;
                    case BooleanLogicType.NotEqual:
                        return floatVal != TargetValue;
                    case BooleanLogicType.LessThan:
                        return floatVal < TargetValue;
                    case BooleanLogicType.LessThanOrEqualTo:
                        return floatVal <= TargetValue;
                    case BooleanLogicType.GreaterThan:
                        return floatVal > TargetValue;
                    case BooleanLogicType.GreaterThanOrEqualTo:
                        return floatVal >= TargetValue;
                    default:
                        throw new Exception( "BooleanLogicType " + this.LogicType + " is not valid for a FloatChecker!" );
                }
            }

            public delegate float GetFloat();
        }
        #endregion FloatChecker

        #region BoolChecker
        public class BoolChecker : ISingleChecker
        {
            public readonly bool TargetValue;
            public readonly BooleanLogicType LogicType;
            public readonly GetBool BoolGetter;

            public BoolChecker( bool TargetValue, BooleanLogicType LogicType, GetBool BoolGetter )
            {
                this.TargetValue = TargetValue;
                this.LogicType = LogicType;
                this.BoolGetter = BoolGetter;
            }

            public bool GetDoesPass()
            {
                bool boolVal = BoolGetter();
                switch ( this.LogicType )
                {
                    case BooleanLogicType.Equals:
                        return boolVal == TargetValue;
                    case BooleanLogicType.NotEqual:
                        return boolVal != TargetValue;
                    default:
                        throw new Exception( "BooleanLogicType " + this.LogicType + " is not valid for a BoolChecker!" );
                }
            }

            public delegate bool GetBool();
        }
        #endregion BoolChecker
    }

    public enum BooleanLogicType
    {
        Equals = 0,
        NotEqual,
        LessThan,
        GreaterThan,
        LessThanOrEqualTo,
        GreaterThanOrEqualTo,
    }
}