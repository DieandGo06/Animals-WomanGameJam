using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
public class IAMove : MonoBehaviour
{
    //BASE DEL CÓDIGO SACADO DE: https://www.youtube.com/watch?v=6BrZryMz-ac&list=LL 
    //Que se base en este artículo que es realmente con el que construí el código http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter18_Context_Steering_Behavior-Driven_Steering_at_the_Macro_Scale.pdf

    #region Clases y Enum
    [Serializable]
    public class TargetMap
    {
        public Transform target;
        public float[] dangerDir = new float[posiblesDirecciones];
        public float[] interestDir = new float[posiblesDirecciones];
    }
    public enum MoveAction { RunAway, Chase, Surround, KeepDirection }
    public enum MapTypes { Interest, Danger, Context }
    #endregion

    [Header("Parametros")]
    public const int posiblesDirecciones = 16;
    [SerializeField] MoveController moveController;
    [SerializeField] float visionRadio;
    [SerializeField] Vector3 offset;

    [Header("Comportamientos")]
    [SerializeField] MoveAction moveAction;
    [SerializeField, Range(0.5f, 5f)] float timerRunAwayToChase;
    [SerializeField, Range(0.1f, 1f)] float timerChaseToAim;

    [Header("Gizmos")]
    [SerializeField] MapTypes mapaMostrado;
    [SerializeField, Range(0f, 1f)] float lineLengthPercentage;

    [Header("Mapas (targets)")]
    [SerializeField] List<TargetMap> targetMaps;


    //Mapas de movimiento 
    float[] filedOfViewMap; 
    float[] interestMap;
    float[] dangerMap;
    public float[] contextMap;
    float lastContextMax;

    //Valores para generar mapas
    float nearbyPosRatio = 0.5f;
    Vector2[] nearbyPositions;
    Vector3 spriteCenter;

    //Timers para cambiar comportamientos
    Utilidades.Timer timer_RunAway_Chase;
    Utilidades.Timer timer_Chase_Aim;


    private RaycastHit2D[] frontRayCast = new RaycastHit2D[1];
    private RaycastHit2D[] leftRayCast = new RaycastHit2D[1];
    private RaycastHit2D[] rightRayCast = new RaycastHit2D[1];
    [SerializeField] LayerMask obstacleLayer;




    private void Awake()
    {
        nearbyPositions = new Vector2[posiblesDirecciones];
        filedOfViewMap = new float[posiblesDirecciones];
        interestMap = new float[posiblesDirecciones];
        contextMap = new float[posiblesDirecciones];
        dangerMap = new float[posiblesDirecciones];
        targetMaps = new List<TargetMap>();

        timer_RunAway_Chase = new Utilidades.Timer(timerRunAwayToChase);
        timer_Chase_Aim = new Utilidades.Timer(timerChaseToAim);
    }

    private void Start()
    {
        moveController = moveController == null ? GetComponent<MoveController>() : moveController;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            moveController.target = collision.transform;

        }

        if (collision.CompareTag("Player") || collision.CompareTag("Wall"))
        {
            TargetMap temp = new TargetMap();
            targetMaps.Add(temp);
            temp.target = collision.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.CompareTag("Wall"))
        {
            if (targetMaps.Count == 0) return;
            foreach (TargetMap mapa in targetMaps)
            {
                if (mapa.target == collision.transform)
                {
                    StartCoroutine(RemoveTargetMap(mapa));
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        //DrawMapaContextual();
        //MarkEnemyToChase();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }


    void ViewField()
    {
        float maxDistance = 2;
        float degreesToTarget = Matematicas.RadianesEntre(transform.position, moveController.target.position) * Mathf.Rad2Deg;

        Vector2 rightDirection; 
        Vector2 leftDirection;
        Debug.DrawRay(moveController.transform.position, moveController.GetMoveDirection() * maxDistance);


        if (Physics2D.RaycastNonAlloc(transform.position, moveController.GetMoveDirection(), frontRayCast, maxDistance, obstacleLayer) != 0)
        {
            float radianToTarget = Matematicas.RadianesEntre(transform.position, moveController.target.position);
        }

    }    



    //----------------------------------------------------------------------------------------------------------------------------------------------------
    #region Metodos publicos: Comportamientos y cambiadores de estados
    public void RunAway()
    {
        if (!AreEnemiesNearby())
        {
            moveController.SetMoveDirection(Vector2.zero);
            return;
        }
        CalculateConextMap();
        //Si no hay buenas opciones, que matenga la direccion
        if (isSurrounded()) return;
        Vector2 newDirection = BestDirection();
        moveController.SetMoveDirection(newDirection);

    }


    public void Chase()
    {
        //if (moveController.target == null || !AreEnemiesNearby())
        //{
        //    moveAction = MoveAction.RunAway;
        //    return;
        //}
        CalculateConextMap();
        float currentMax = contextMap.Max();
        //Evita que cambie constantemente de direccion por una pequeña variacion
        //if (currentMax >= lastContextMax - 0.02f && currentMax <= lastContextMax + 0.02f) return;
        lastContextMax = contextMap.Max();
        Vector2 newDirection = BestDirection();
        moveController.SetMoveDirection(newDirection);
    }

    public void Surround()
    {
        if (moveController.target == null || !AreEnemiesNearby())
        {
            moveAction = MoveAction.RunAway;
            return;
        }
        CalculateConextMap();
        float currentMax = contextMap.Max();
        //Evita que cambie constantemente de direccion por una pequeña variacion
        if (currentMax >= lastContextMax - 0.02f && currentMax <= lastContextMax + 0.02f) return;
        lastContextMax = contextMap.Max();
        Vector2 newDirection = BestDirection();
        moveController.SetMoveDirection(newDirection);
    }

    public IEnumerator AimAndAttack()
    {
        //if (controller.target == null) yield break;
        ////Se usa Vector2.zero para normalizar el vector
        //float angleWeaponToTarget = Matematicas.RadianesEntre(controller.weapon.transform.position, controller.target.transform.position);
        //float angleBodyToTarget = Matematicas.RadianesEntre(controller.transform.position, controller.target.transform.position);
        //Vector2 aimDirection = Matematicas.PolaresToRectangulares(1, angleWeaponToTarget, Vector2.zero);
        //yield return new WaitForEndOfFrame();
        //controller.SetMoveDirection(aimDirection);
        //moveAction = MoveAction.KeepDirection;
        //yield return new WaitForSeconds(0.05f);
        //controller.Attack.Invoke();
        yield break;
    }

    public void SelectTarget()
    {
        //1. Se indentifica la direccion donde en promedio hay mas enemigos
        //2. Busca al primer enemigo que encuentre más cercano a esa direccion
        if (!AreEnemiesNearby()) return;
        if (contextMap[BestDrectionIndex()] < -0.1f) return;
        float[] averageDangerMap = AverageMapOf(MapTypes.Danger);
        int indexMaxAverage = Array.IndexOf(averageDangerMap, averageDangerMap.Max());

        foreach (TargetMap mapa in targetMaps)
        {
            int indexMaxOfTarget = Array.IndexOf(mapa.dangerDir, mapa.dangerDir.Max());
            if (indexMaxOfTarget == indexMaxAverage)
            {
                moveController.target = mapa.target;
                moveAction = MoveAction.Chase;
                return;
            }
        }
    }

    public bool isReadyToChase()
    {
        timer_RunAway_Chase.Play();
        if (timer_RunAway_Chase.finished())
        {
            timer_RunAway_Chase.Reset();
            return true;
        }
        else return false;
    }

    public bool isReadyToAim()
    {
        timer_Chase_Aim.Play();
        if (timer_Chase_Aim.finished())
        {
            timer_Chase_Aim.Reset();
            return true;
        }
        else return false;
    }

    public MoveAction GetMoveAction() { return moveAction; }
    public void SetMoveAction(MoveAction _moveAction) { moveAction = _moveAction; }
    #endregion
    //----------------------------------------------------------------------------------------------------------------------------------------------------


    //----------------------------------------------------------------------------------------------------------------------------------------------------
    #region Generador de mapas
    void CalculateConextMap()
    {
        if (!AreEnemiesNearby()) return;
        CalculateNearbyPositions();
        CalculateInterestMap();
        CalculateDangerMap();
        ViewField();

        for (int i = 0; i < posiblesDirecciones; i++)
        {
            contextMap[i] = interestMap[i] - (dangerMap[i]*0.5f);
        }
    }


    void CalculateDangerMap()
    {
        ResetMap(dangerMap);
        foreach (TargetMap mapa in targetMaps)
        {
            for (int i = 0; i < posiblesDirecciones; i++)
            {
                Vector2 start = nearbyPositions[i];
                if (moveAction == MoveAction.RunAway)
                {
                    //Entre mas de frente este, mayor es el peso
                    float angleWeight = AngleWeight(start, mapa.target.position);
                    //Invierto. Entre mas cerca, mayor es el peso
                    float distanceWeight = 1 - DistanceWeight(start, mapa.target.position);
                    mapa.dangerDir[i] = (angleWeight * 0.5f) + (distanceWeight * 0.5f);
                }

                if (moveAction == MoveAction.Chase)
                {
                    if (mapa.target == moveController.target) continue;
                    //Entre mas de frente este, mayor es el peso
                    float angleWeight = AngleWeight(start, mapa.target.position);
                    //Invierto. Entre mas cerca, mayor es el peso
                    float distanceWeight = 1 - DistanceWeight(start, mapa.target.position);
                    mapa.dangerDir[i] = (angleWeight * 0.3f) + (distanceWeight * 0.7f);
                }

                if (moveAction == MoveAction.Surround)
                {
                    if (mapa.target == moveController.target) continue;
                    //Entre mas de frente este, mayor es el peso
                    float angleWeight = AngleWeight(start, mapa.target.position);
                    //Invierto. Entre mas cerca, mayor es el peso
                    float distanceWeight = 1 - DistanceWeight(start, mapa.target.position);
                    mapa.dangerDir[i] = (angleWeight * 0.3f) + (distanceWeight * 0.7f);
                }
            }
        }
        //dangerMap = AverageMapOf(MapTypes.Danger);
        //Se queda con los maximos
        GetTheMostDangerous();
    }


    void CalculateInterestMap()
    {
        ResetMap(interestMap);
        foreach (TargetMap mapa in targetMaps)
        {
            for (int i = 0; i < posiblesDirecciones; i++)
            {
                Vector2 start = nearbyPositions[i];
                if (moveAction == MoveAction.RunAway)
                {
                    //Invierto. Entre mas de espalda, mayor es el peso
                    float angleWeight = 1 - AngleWeight(start, mapa.target.position);
                    float currentDirectionWeight = CurrentDirectionWeight(start);
                    mapa.interestDir[i] = (angleWeight * 0.7f) + (currentDirectionWeight * 0.3f);
                }

                if (moveAction == MoveAction.Chase)
                {
                    if (mapa.target != moveController.target) continue;
                    //Entre mas de frente, mayor es el peso
                    float angleWeight = AngleWeight(start, mapa.target.position);
                    //Favorece a los costados
                    angleWeight = 1 - Mathf.Abs(angleWeight - 0.8f);
                    //Entre mas lejos, mayor es el peso
                    float distanceWeight = DistanceWeight(start, mapa.target.position);
                    //Favorece a mantener la misma direccion
                    float currentDirectionWeight = CurrentDirectionWeight(start);

                    mapa.interestDir[i] = (angleWeight * 0.9f) + (currentDirectionWeight * 0.1f);
                    interestMap[i] = mapa.interestDir[i];
                }

                if (moveAction == MoveAction.Surround)
                {
                    if (mapa.target != moveController.target) continue;
                    float minDistToSurround = 0.7f;
                    //Entre mas lejos, mayor es el peso
                    float distanceWeight = DistanceWeight(start, mapa.target.position);
                    //Si "distanceWeight" supera al valor minimo, se reduce el interest
                    distanceWeight = distanceWeight >= minDistToSurround ? distanceWeight - 1 : distanceWeight;
                    //Favorece a mantener la misma direccion
                    float currentDirectionWeight = CurrentDirectionWeight(start);

                    mapa.interestDir[i] = (distanceWeight * 0.6f) + (currentDirectionWeight * 0.4f);
                    interestMap[i] = mapa.interestDir[i];
                }
            }
        }

        if (moveAction == MoveAction.RunAway)
        {
            interestMap = AverageMapOf(MapTypes.Interest);
        }
    }
    #endregion
    //----------------------------------------------------------------------------------------------------------------------------------------------------


    //----------------------------------------------------------------------------------------------------------------------------------------------------
    #region Metodos auxiliares a la generación de mapas
    float[] AverageMapOf(MapTypes mapToAverage)
    {
        if (mapToAverage == MapTypes.Context)
        {
            UnityEngine.Debug.LogWarning("No se puede promediar el mapa contextual");
            return null;
        }

        float[] tempMap = new float[posiblesDirecciones];
        foreach (TargetMap mapa in targetMaps)
        {
            for (int i = 0; i < posiblesDirecciones; i++)
            {
                if (mapToAverage == MapTypes.Interest)
                    tempMap[i] += mapa.interestDir[i];

                if (mapToAverage == MapTypes.Danger)
                    tempMap[i] += mapa.dangerDir[i];
            }
        }
        for (int i = 0; i < posiblesDirecciones; i++)
            tempMap[i] = tempMap[i] / targetMaps.Count;

        return tempMap;
    }

    void GetTheMostDangerous()
    {
        //Se queda las 3 direcciones mas peligrosas con cada "mapTarget"
        //Si las direcciones se repiten, se queda con el valor más alto
        //--------------------------------------------------------------------
        foreach (TargetMap mapa in targetMaps)
        {
            int dangerestDirIndex = Array.FindIndex(mapa.dangerDir, (x) => x == mapa.dangerDir.Max());
            int indexBefore = dangerestDirIndex != 0 ? dangerestDirIndex - 1 : posiblesDirecciones - 1;
            int indexAfter = dangerestDirIndex != posiblesDirecciones - 1 ? dangerestDirIndex + 1 : 0;

            if (mapa.dangerDir[indexBefore] > dangerMap[indexBefore])
                dangerMap[indexBefore] = mapa.dangerDir[indexBefore];

            if (mapa.dangerDir[dangerestDirIndex] > dangerMap[dangerestDirIndex])
                dangerMap[dangerestDirIndex] = mapa.dangerDir[dangerestDirIndex];

            if (mapa.dangerDir[indexAfter] > dangerMap[indexAfter])
                dangerMap[indexAfter] = mapa.dangerDir[indexAfter];
        }
    }

    void ResetMap(float[] map)
    {
        for (int i = 0; i < map.Length; i++)
        {
            map[i] = 0;
        }
    }

    bool AreEnemiesNearby()
    {
        if (targetMaps.Count == 0) return false;
        else return true;
    }

    IEnumerator RemoveTargetMap(TargetMap _mapa)
    {
        yield return new WaitForSeconds(0.5f);
        yield return new WaitForEndOfFrame();
        if (_mapa.target == moveController.target) moveController.target = null;
        targetMaps.Remove(_mapa);
    }

    Vector2 BestDirection()
    {
        float bestDirectionIndex = Array.FindIndex(contextMap, (x) => x == contextMap.Max());
        float directionAngle = ((360 / posiblesDirecciones) * bestDirectionIndex) * Mathf.Deg2Rad;
        return Matematicas.PolaresToRectangulares(1, directionAngle, Vector2.zero);
    }

    int BestDrectionIndex()
    {
        return Array.FindIndex(contextMap, (x) => x == contextMap.Max());
    }

    void CalculateNearbyPositions()
    {
        spriteCenter = moveController.transform.position + offset;
        for (int i = 0; i < posiblesDirecciones; i++)
        {
            float angle = (360f / posiblesDirecciones) * i;
            nearbyPositions[i] = Matematicas.PolaresToRectangulares(nearbyPosRatio, angle * Mathf.Deg2Rad, spriteCenter);
        }
    }

    public bool isSurrounded()
    {
        if (contextMap[BestDrectionIndex()] < -0.15f) return true;
        else return false;
    }

    #endregion
    //----------------------------------------------------------------------------------------------------------------------------------------------------


    //----------------------------------------------------------------------------------------------------------------------------------------------------
    #region Calculo de pesos para mapas 
    float AngleWeight(Vector2 _startPos, Vector2 _target)
    {
        //Vector.Dot te permite saber el angulo entre dos vectores si estan normalizados.
        //Código sacado de la documentación: https://docs.unity3d.com/ScriptReference/Vector3.Dot.html
        //---------------------------------------------------------------------------------------------

        //Vector.Dot funciona de la forma que quiero solo con vectores normalizados
        Vector3 localPosNormalized = Vector3.Normalize((spriteCenter * Vector2.one) - _startPos);
        Vector2 targetDirectionNormalized = Vector3.Normalize(_startPos - _target);
        //Angulo dados de (-1,1): "-1" detras, "1" de frente
        float angleToTarget = Vector2.Dot(localPosNormalized, targetDirectionNormalized);
        //Entre mas de frente, mayor es el peso
        angleToTarget = Matematicas.Map(angleToTarget, -1, 1, 0, 1);
        return angleToTarget;
    }

    float DistanceWeight(Vector2 _startPos, Vector2 _target)
    {
        //Vector.Distance es... "(v1-v2).magnitude". La magnitud se calcula con una raiz cuadrada, operación que consume muchos recursos
        //Por ello se usa "sqrMagnitude" (cancela la raiz con otra raiz). https://docs.unity3d.com/ScriptReference/Vector3-sqrMagnitude.html
        //---------------------------------------------------------------------------------------------

        //Distancia al cuadrado
        float distance = (_startPos - _target).sqrMagnitude;
        //"maxEscala1" al cuadrado para compensar la distancia al cuadrado
        float distanceWeight = Matematicas.Map(distance, 0, Mathf.Pow(visionRadio, 2), 0, 1);
        //Mientras mas lejos, mayor es el peso
        distanceWeight = Mathf.Clamp(distanceWeight, 0, 1);
        return distanceWeight;
    }

    float CurrentDirectionWeight(Vector2 _startPos)
    {
        //la idea es ayudar a mantener la dirección actual o similares para evitar trabajas e indesición
        //---------------------------------------------------------------------------------------------

        //Vector.Dot funciona de la forma que quiero solo con vectores normalizados
        Vector3 localPosNormalized = Vector3.Normalize((spriteCenter * Vector2.one) - _startPos);
        //Devuelve valores invertidos. "-1" si mantiene la dirección y "1" si va a la opuesta
        float moveDirectionWeight = Vector2.Dot(localPosNormalized, moveController.GetMoveDirection());
        moveDirectionWeight = Matematicas.Map(moveDirectionWeight, -1, 1, 0, 1);
        //Normalizo e invierto para tener lso valores correctos
        moveDirectionWeight = 1 - moveDirectionWeight;
        return moveDirectionWeight;
    }
    #endregion
    //----------------------------------------------------------------------------------------------------------------------------------------------------


    //----------------------------------------------------------------------------------------------------------------------------------------------------
    #region Dibujar mapas copn Gizmos
    void DrawMapaContextual()
    {
        if (targetMaps.Count == 0) return;

        //Rango de vision
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(spriteCenter, visionRadio);

        //Representacion de mapa
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(spriteCenter, nearbyPosRatio);

        //Mapas mostrado
        float[] displayedMap = contextMap;
        if (mapaMostrado == MapTypes.Danger) displayedMap = dangerMap;
        if (mapaMostrado == MapTypes.Interest) displayedMap = interestMap;

        //Colores: Default
        Color good = Color.green;
        Color mid = Color.yellow;
        Color bad = Color.red;
        //Danger map
        if (mapaMostrado == MapTypes.Danger)
        {
            good = Color.red;
            mid = Color.yellow;
            bad = Color.green;
        }

        for (int i = 0; i < posiblesDirecciones; i++)
        {
            //Los valores pueden ser hasta -1, pero visualmente necesito que lleguen a 0
            displayedMap[i] = displayedMap[i] <= 0 ? 0.1f + UnityEngine.Random.Range(0f, 0.03f) : displayedMap[i];

            Vector2 start = nearbyPositions[i];
            float angle = (360f / posiblesDirecciones) * i;
            float ratio = ((displayedMap[i] * visionRadio) - nearbyPosRatio);
            Vector2 end = Matematicas.PolaresToRectangulares(ratio * lineLengthPercentage, angle * Mathf.Deg2Rad, start);

            if (displayedMap[i] >= 0 && displayedMap[i] < 0.3f)
                Gizmos.color = bad;
            else if (displayedMap[i] >= 0.3 && displayedMap[i] < 0.7f)
                Gizmos.color = mid;
            else if (displayedMap[i] >= 0.7)
                Gizmos.color = good;

            Gizmos.DrawLine(start, end);

            //Si es la idreccion que esta tomando, dibuja un circulo en la punta
            if (i == BestDrectionIndex())
            {
                Gizmos.DrawWireSphere(end, 0.3f);
            }
        }
    }

    void MarkEnemyToChase()
    {
        if (moveController.target != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(moveController.target.position, 0.5f);
        }
    }
    #endregion
    //----------------------------------------------------------------------------------------------------------------------------------------------------
}
