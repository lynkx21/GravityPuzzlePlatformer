using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using MoreMountains.CorgiEngine;
using UnityEngine;

namespace LudOfficina.GravityPuzzlePlatformer
{
    /// <summary>
    /// Add this class to a Projectile and it'll be able to be attracted by Gravity Masses
    /// </summary>
    [RequireComponent(typeof(ProjectileGravity))]
    public class ProjectileGravityAbility : MonoBehaviour
    {
        public enum TransitionForcesModes { Reset, Adapt, Nothing }

        [Header("Influence")]
        /// If this is true, this Projectile will be attracted
        public bool subjectToGravityPoints = true;

        [Header("[Experimental] Rotation")]
        /// the speed at which the Character rotates to match its new gravity's rotation. 0 means instant.
		public float RotationSpeed = 0f;
        /// the duration (in seconds) during which a zone is ignored when entered/exited, right after the enter/exit, to accomodate for rotation times. If you have a slow rotation speed, increase this.
        public float InactiveBufferDuration = 0.1f;

        [Header("Transition")]
        /// if this is set to true, forces will be reset when entering/exiting gravity zones.
        public TransitionForcesModes TransitionForcesMode = TransitionForcesModes.Reset;

        public float GravityAngle { get { return _gravityOverridden ? _overrideGravityAngle : _defaultGravityAngle; } }

        protected ProjectileGravity projectile;

        protected List<GravityPoint> _gravityPoints;
        protected GravityPoint _closestGravityPoint = null;
        protected Vector2 _gravityPointDirection = Vector2.zero;

        protected float _defaultGravityAngle = 0f;
        protected float _currentGravityAngle;
        protected float _overrideGravityAngle = 0f;

        protected bool _gravityOverridden = false;

        protected float _rotationDirection = 0f;
        protected const float _rotationSpeedMultiplier = 1000f;

        protected Vector3 _newRotationAngle = Vector3.zero;

        protected float _entryTimeStampZones = 0f;
        protected float _entryTimeStampPoints = 0f;
        protected GravityPoint _lastGravityPoint = null;
        protected GravityPoint _newGravityPoint = null;

        protected float _previousGravityAngle;


        // Use this for initialization
        void Start()
        {
            projectile = GetComponent<ProjectileGravity>();

            _gravityPoints = new List<GravityPoint>();
            UpdateGravityPointsList();
            
        }

        // Update is called once per frame
        void Update()
        {
            ComputeGravityPoints();
            NaiveUpdateGravity();
        }

        protected virtual void NaiveUpdateGravity()
        {
            if (_gravityOverridden && _lastGravityPoint != null)
            {
                Vector3 originalDirection = projectile.Direction; 
                Vector3 gravityDirection = (_lastGravityPoint.transform.position - this.transform.position);
                //Debug.Log(originalDirection);
                //Debug.Log(direction.normalized);
                Vector3 newDirection = (gravityDirection - originalDirection).normalized;
                float distance = newDirection.magnitude;
                float attractionForce = 1 / distance * 6.678f;
                this.transform.Translate(newDirection.normalized * attractionForce * Time.deltaTime, Space.World);
                projectile.Direction = newDirection;
            }
        }

        protected virtual void UpdateGravity()
        {
            if (RotationSpeed == 0)
            {
                _currentGravityAngle = GravityAngle;
            }
            else
            {
                float gravityAngle = GravityAngle;
                // if there's a 180° difference between both angles, we force the rotation angle depending on the direction of the character
                if (Mathf.DeltaAngle(_currentGravityAngle, gravityAngle) == 180)
                {

                    _currentGravityAngle = _currentGravityAngle % 360;


                    if (_rotationDirection > 0)
                    {
                        _currentGravityAngle += 0.1f;
                    }
                    else
                    {
                        _currentGravityAngle -= 0.1f;
                    }
                }

                if (Mathf.DeltaAngle(_currentGravityAngle, gravityAngle) > 0)
                {
                    if (Mathf.Abs(Mathf.DeltaAngle(_currentGravityAngle, gravityAngle)) < Time.deltaTime * RotationSpeed * _rotationSpeedMultiplier)
                    {
                        _currentGravityAngle = gravityAngle;
                    }
                    else
                    {
                        _currentGravityAngle += Time.deltaTime * RotationSpeed * _rotationSpeedMultiplier;
                    }
                }
                else
                {
                    if (Mathf.Abs(Mathf.DeltaAngle(_currentGravityAngle, gravityAngle)) < Time.deltaTime * RotationSpeed * _rotationSpeedMultiplier)
                    {
                        _currentGravityAngle = gravityAngle;
                    }
                    else
                    {
                        _currentGravityAngle -= Time.deltaTime * RotationSpeed * _rotationSpeedMultiplier;
                    }
                }

            }
            _newRotationAngle.z = _currentGravityAngle;
            transform.localEulerAngles = _newRotationAngle;
            if (_gravityOverridden && (_lastGravityPoint != null))
            {
                transform.Translate(_lastGravityPoint.transform.position * Time.deltaTime * .005f, Space.World);
            }
        }

        protected virtual void ComputeGravityPoints()
        {
            // if not affected by gravity ponts, do nothing and exit
            if (!subjectToGravityPoints) { return; }

            // grab closest gravity point
            _closestGravityPoint = GetClosestGravityPoint();
            Debug.Log(_closestGravityPoint.transform.position);

            // if it's not null
            if (_closestGravityPoint != null)
            {
                _newGravityPoint = (_lastGravityPoint == null) ? _closestGravityPoint : _lastGravityPoint;
                // if we have a new gravity point
                if ((_lastGravityPoint != _closestGravityPoint) && (_lastGravityPoint != null))
                {
                    // time check to switch gravity point
                    if (Time.time - _entryTimeStampPoints >= InactiveBufferDuration)
                    {
                        _entryTimeStampPoints = Time.time;
                        _newGravityPoint = _closestGravityPoint;
                        Transition(true, _newGravityPoint.transform.position - this.transform.position);
                        // StartRotating();
                    }
                }
                // if we didn't have a gravity point last time
                if (_lastGravityPoint == null)
                {
                    // time check to switch gravity point
                    if (Time.time - _entryTimeStampPoints >= InactiveBufferDuration)
                    {
                        _entryTimeStampPoints = Time.time;
                        _newGravityPoint = _closestGravityPoint;
                        Transition(true, _newGravityPoint.transform.position - this.transform.position);
                        // StartRotating();
                    }
                }
                // ovveride gravity
                _gravityPointDirection = _newGravityPoint.transform.position - this.transform.position;
                float gravityAngle = 180 - MMMaths.AngleBetween(Vector2.up, _gravityPointDirection);
                _gravityOverridden = true;
                _overrideGravityAngle = gravityAngle;
                _lastGravityPoint = _newGravityPoint;
            }
            else
            {
                // without gravity point in range, our gravity is not overridden
                if (Time.time - _entryTimeStampPoints >= InactiveBufferDuration)
                {
                    if (_lastGravityPoint != null)
                    {
                        Transition(false, _newGravityPoint.transform.position - this.transform.position);
                        // StartRotating();
                    }
                    _entryTimeStampPoints = Time.time;
                    _gravityOverridden = false;
                    _lastGravityPoint = null;
                }
            }
        }

        protected virtual GravityPoint GetClosestGravityPoint()
        {
            if (_gravityPoints.Count == 0)
            {
                return null;
            }

            GravityPoint closestGravityPoint = null;
            float closestDistanceSqr = Mathf.Infinity;
            //Vector3 currentPosition = _controller.ColliderCenterPosition; // ??
            Vector3 currentPosition = this.transform.position;

            foreach (GravityPoint gravityPoint in _gravityPoints)
            {
                Vector3 directionToTarget = gravityPoint.transform.position - currentPosition;
                float dSqrToTarget = directionToTarget.sqrMagnitude;

                // if outside this point's zone of effect, we do nothing and exit
                if (directionToTarget.magnitude > gravityPoint.DistanceOfEffect)
                {
                    continue;
                }

                if(dSqrToTarget < closestDistanceSqr)
                {
                    closestDistanceSqr = dSqrToTarget;
                    closestGravityPoint = gravityPoint;
                }
            }

            //Debug.Log(closestGravityPoint.transform.position);
            return closestGravityPoint;
        }

        public virtual void UpdateGravityPointsList()
        {
            if(_gravityPoints.Count != 0)
            {
                _gravityPoints.Clear();
            }

            _gravityPoints.AddRange(FindObjectsOfType(typeof(GravityPoint)) as GravityPoint[]);
            //Debug.Log(_gravityPoints.Count);
        }

        protected virtual void Transition(bool entering, Vector2 gravityDirection)
        {
            float gravityAngle = 180 - MMMaths.AngleBetween(Vector2.up, gravityDirection);
            if(TransitionForcesMode == TransitionForcesModes.Nothing)
            {
                return;
            }
            if (TransitionForcesMode == TransitionForcesModes.Reset)
            {
                return;
                //_controller.SetForce(Vector2.zero);
                //_movement.ChangeState(CharacterStates.MovementStates.Idle);
            }
            if (TransitionForcesMode == TransitionForcesModes.Adapt)
            {
                // the angle is calculated depending on if the player enters or exits a zone and takes _previousGravityAngle as parameter if you glide over from one zone to another
                float rotationAngle = entering ? _previousGravityAngle - gravityAngle : gravityAngle - _defaultGravityAngle;
                return;
                //_controller.SetForce(Quaternion.Euler(0, 0, rotationAngle) * _controller.Speed);
            }
            _previousGravityAngle = entering ? gravityAngle : _defaultGravityAngle;

        }
    }
}