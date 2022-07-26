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

    /// <summary>
    /// This is just test code, for showing how this works
    /// </summary>
    public static class BooleanLogicCheckerTreeExampleCode
    {
        public static void TestSomeLogic()
        {
            //we are going to express the following C# statement with a BooleanLogicCheckerTree:
            //
            //bool IsIntSetting = (type == “IntTextbox” || type == “IntHidden” || type == “IntDropdown” ||
            //    (type == “IntSlider” && otherfield > 5 && otherfield <= 40 ) );
            //
            //the xml that could populate this (given an xml parser) is available here: https://docs.google.com/document/d/1r9tFH0aHq8kfyhAzoWIMxkDEmXhHdcr8pko9qc-CoDY/edit?usp=sharing


            //this is our fake data we are going to check against
            string type = "IntTextbox";
            int otherfield = 10;

            //this is our logic tree that we would have built from parsing xml, normally
            BooleanLogicCheckerTree logicTree = new BooleanLogicCheckerTree( "IsIntSetting", new BooleanLogicCheckerTree.OrGroup() );
            //we are using delegates like this so that we can easily use this checker tree with any sibling sort of data structure
            logicTree.RootGroup.DirectCheckers.Add( new BooleanLogicCheckerTree.StringChecker( "IntTextbox", BooleanLogicType.Equals, delegate { return type; } ) );
            logicTree.RootGroup.DirectCheckers.Add( new BooleanLogicCheckerTree.StringChecker( "IntHidden", BooleanLogicType.Equals, delegate { return type; } ) );
            logicTree.RootGroup.DirectCheckers.Add( new BooleanLogicCheckerTree.StringChecker( "IntDropdown", BooleanLogicType.Equals, delegate { return type; } ) );

            BooleanLogicCheckerTree.AndGroup andGroup = new BooleanLogicCheckerTree.AndGroup();
            logicTree.RootGroup.SubGroups.Add( andGroup );

            andGroup.DirectCheckers.Add( new BooleanLogicCheckerTree.StringChecker( "IntSlider", BooleanLogicType.Equals, delegate { return type; } ) );
            andGroup.DirectCheckers.Add( new BooleanLogicCheckerTree.IntChecker( 5, BooleanLogicType.GreaterThan, delegate { return otherfield; } ) );
            andGroup.DirectCheckers.Add( new BooleanLogicCheckerTree.IntChecker( 40, BooleanLogicType.LessThanOrEqualTo, delegate { return otherfield; } ) );

            //now that all the setup is done, we can call this as much as we want:
            logicTree.GetDoesPass();
            //that should return true, at the moment.

            //but we can change the values in the original data, and make further calls at will, like this:
            otherfield = 2;

            //now when we call this, without making any other changes, this should return false
            //once the BooleanLogicCheckerTree is set up once, it doesn't have to be altered anymore in order to give you
            //the proper pass/fail for actual data that it is looking at.  The data itself just needs to be properly returned
            //by the delegates that we're passing in to the tree in the first place.
            logicTree.GetDoesPass();
        }
    }
}