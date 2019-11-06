using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Random = UnityEngine.Random;

/// <summary>
/// Sets the block type
/// ERROR would allow for error detection but needs more time to implement properly
/// </summary>
public enum EGameBoardType
{
    ERROR,
    Empty,
    Enemy,
    Start,
    Finish
}

/// <summary>
/// Use this class to generate the data for the board
/// There will be a renderer working beside this to update positions when necessary
/// m_acGameBoardData will be initalised with a custom 2 dimentional value designated by the level the player reaches
/// Each square will contain data stored as a GameBoardElement class and will store the characteristics of the square as well as addresses to each accessible square around it
/// The optimal path will be randomly generated starting from the start point and will have to satify a set of criteria for it to be a valid level
/// </summary>
public class GameBoardGenerator : MonoBehaviour
{
    //Editor Accessors without it being public to avoid misuse
    [SerializeField]
    GameObject m_gSquareBackground;
    [SerializeField]
    GameObject m_gPlayerObject;
    [SerializeField]
    GameObject m_gEnemyObject;
    [SerializeField]
    GameObject m_gStartObject;
    [SerializeField]
    GameObject m_gFinishObject;

    [SerializeField]
    TextMeshProUGUI m_cLevelCounter;
    [SerializeField]
    TextMeshProUGUI m_cStatusMessage;
    [SerializeField]
    TextMeshProUGUI m_cButtonMessage;
    [SerializeField]
    Button m_cProgressButton;
    [SerializeField]
    GameObject m_gRootStatus;

    int m_iBoardWidth = 4;
    int m_iBoardHeight = 4;
    int m_iNumberOfEnemies = 1;
    int m_iOptimalStepCount = 6;

    int m_iCurrentLevel = 1;

    GameBoardElement[,] m_acGameBoardData;

    GameObject m_gActivePlayer;
    Player m_cPlayerScript;

    GameBoardElement m_gStartLocation;

    bool m_bIsGamePlaying;
    bool m_bSmallThresholdPassed;
    bool m_bMediumThresholdPassed;

    float m_fSquareWidth;
    float m_fSquareHeight;

    void Start()
    {
        BoardSetUp();
        m_bIsGamePlaying = true;
        m_bSmallThresholdPassed = false;
        m_bMediumThresholdPassed = false;
    }

    void Update()
    {
        if(m_bIsGamePlaying)
        {
            InputSystem();
        }
    }

    /// <summary>
    /// Triggers the level complete result
    /// </summary>
    void LevelCompleted()
    {
        m_bIsGamePlaying = false;
        m_gRootStatus.SetActive(true);
        m_cStatusMessage.text = "Level Complete";
        m_cButtonMessage.text = "Next Level";

        m_cProgressButton.onClick.RemoveAllListeners();
        m_cProgressButton.onClick.AddListener(NextLevelButton);
    }

    /// <summary>
    /// Triggers the level failed result
    /// </summary>
    void LevelFailed()
    {
        m_bIsGamePlaying = false;
        m_gRootStatus.SetActive(true);
        m_cStatusMessage.text = "Game Over";
        m_cButtonMessage.text = "Retry";

        m_cProgressButton.onClick.RemoveAllListeners();
        m_cProgressButton.onClick.AddListener(RetryButton);
    }

    /// <summary>
    /// Functionality to move to the next level
    /// </summary>
    void NextLevelButton()
    {
        BoardClearup();
        m_iCurrentLevel++;
        m_iNumberOfEnemies++;
        if(m_iCurrentLevel >= 5 && !m_bSmallThresholdPassed)
        {
            m_iBoardWidth = 5;
            m_iBoardHeight = 5;
            m_iNumberOfEnemies = 3;
            m_bSmallThresholdPassed = true;
        }
        if (m_iCurrentLevel >= 10 && !m_bMediumThresholdPassed)
        {
            m_iBoardWidth = 6;
            m_iBoardHeight = 6;
            m_iNumberOfEnemies = 5;
            m_bMediumThresholdPassed = true;
        }

        if(m_iNumberOfEnemies > 10)
        {
            m_iNumberOfEnemies = 10;
        }

        BoardSetUp();
        m_bIsGamePlaying = true;
    }

    /// <summary>
    /// Functionality to reset the game
    /// </summary>
    void RetryButton()
    {
        m_cPlayerScript.m_cSquareBelowPlayer = m_gStartLocation;
        RectTransform cSquareTransform = m_gStartLocation.m_gConnectedGameObject.GetComponent<RectTransform>();
        m_gActivePlayer.GetComponent<RectTransform>().localPosition = new Vector3(cSquareTransform.localPosition.x + (cSquareTransform.sizeDelta.x * 0.5f), cSquareTransform.localPosition.y - (cSquareTransform.sizeDelta.y * 0.5f), 0.0f);

        m_gRootStatus.SetActive(false);
        m_bIsGamePlaying = true;
    }

    /// <summary>
    /// Moved input system to its own function to make it easier to read
    /// </summary>
    void InputSystem()
    {
        //Makes sure the player script is active
        if (m_cPlayerScript != null)
        {
            //Basic, Quick input checks
            if (Input.GetButtonDown("Left"))
            {
                if (m_cPlayerScript.m_cSquareBelowPlayer.m_cTopElement != null)
                {
                    m_cPlayerScript.m_cSquareBelowPlayer = m_cPlayerScript.m_cSquareBelowPlayer.m_cTopElement;
                }
            }

            if (Input.GetButtonDown("Right"))
            {
                if (m_cPlayerScript.m_cSquareBelowPlayer.m_cBotElement != null)
                {
                    m_cPlayerScript.m_cSquareBelowPlayer = m_cPlayerScript.m_cSquareBelowPlayer.m_cBotElement;
                }
            }

            if (Input.GetButtonDown("Up"))
            {
                if (m_cPlayerScript.m_cSquareBelowPlayer.m_cLeftElement != null)
                {
                    m_cPlayerScript.m_cSquareBelowPlayer = m_cPlayerScript.m_cSquareBelowPlayer.m_cLeftElement;
                }
            }

            if (Input.GetButtonDown("Down"))
            {
                if (m_cPlayerScript.m_cSquareBelowPlayer.m_cRightElement != null)
                {
                    m_cPlayerScript.m_cSquareBelowPlayer = m_cPlayerScript.m_cSquareBelowPlayer.m_cRightElement;
                }
            }

            //Player is moved here
            m_gActivePlayer.transform.SetAsLastSibling();
            RectTransform cSquareTransform = m_cPlayerScript.m_cSquareBelowPlayer.m_gConnectedGameObject.GetComponent<RectTransform>();
            m_gActivePlayer.GetComponent<RectTransform>().localPosition = new Vector3(cSquareTransform.localPosition.x + (cSquareTransform.sizeDelta.x * 0.5f), cSquareTransform.localPosition.y - (cSquareTransform.sizeDelta.y * 0.5f), 0.0f);
            
            //Checks the players condition
            if (m_cPlayerScript.m_cSquareBelowPlayer.m_eElementType == EGameBoardType.Enemy)
            {
                LevelFailed();
            }

            if (m_cPlayerScript.m_cSquareBelowPlayer.m_eElementType == EGameBoardType.Finish)
            {
                LevelCompleted();
            }
        }
    }

    /// <summary>
    /// Sets up the board with the current set options
    /// </summary>
    void BoardSetUp()
    {
        m_gRootStatus.SetActive(false);
        FormBoardData();
        CreateStartAndFinishPoints();
        CreateEnemies();
        FullGameBoardRender();
        m_cLevelCounter.text = "Level: " + m_iCurrentLevel.ToString();
    }

    /// <summary>
    /// Keeps the array and gameobjects clear
    /// </summary>
    void BoardClearup()
    {
        Array.Clear(m_acGameBoardData, 0, m_acGameBoardData.Length);

        foreach(Transform t in transform)
        {
            Destroy(t.gameObject);
        }
    }

    /// <summary>
    /// Initialises Board Data
    /// </summary>
    void FormBoardData()
    {
        m_acGameBoardData = new GameBoardElement[m_iBoardWidth, m_iBoardHeight];

        for (int i = 0; i < m_iBoardWidth; i++)
        {
            for (int j = 0; j < m_iBoardHeight; j++)
            {
                //Initialises the Class to store data
                GameBoardElement cNewElement = new GameBoardElement(i, j)
                {
                    m_eElementType = EGameBoardType.Empty
                };

                if (j - 1 >= 0 && m_acGameBoardData[i, j - 1] != null)
                {
                    cNewElement.m_cTopElement = m_acGameBoardData[i, j - 1];
                    m_acGameBoardData[i, j - 1].m_cBotElement = cNewElement;
                }

                if (j + 1 < m_iBoardHeight && m_acGameBoardData[i, j + 1] != null)
                {
                    cNewElement.m_cBotElement = m_acGameBoardData[i, j + 1];
                    m_acGameBoardData[i, j + 1].m_cTopElement = cNewElement;
                }

                if (i - 1 >= 0 && m_acGameBoardData[i - 1, j] != null)
                {
                    cNewElement.m_cLeftElement = m_acGameBoardData[i - 1, j];
                    m_acGameBoardData[i - 1, j].m_cRightElement = cNewElement;
                }

                if (i + 1 < m_iBoardWidth && m_acGameBoardData[i + 1, j] != null)
                {
                    cNewElement.m_cRightElement = m_acGameBoardData[i + 1, j];
                    m_acGameBoardData[i + 1, j].m_cLeftElement = cNewElement;
                }

                m_acGameBoardData[i, j] = cNewElement;
            }
        }
    }

    /// <summary>
    /// Creates the start and finish points
    /// Note: No path checks have been implemented to make the game more interesting, the finish can spawn very close to the start.
    /// With more time, algoritims can be put into place to improve the quaity and consistency of the games.
    /// </summary>
    void CreateStartAndFinishPoints()
    {
        int iRandomStartX = Random.Range(0, m_iBoardWidth);
        int iRandomStartY = Random.Range(0, m_iBoardHeight);

        GameBoardElement cStartEle = m_acGameBoardData[iRandomStartX, iRandomStartY];
        m_gStartLocation = cStartEle;

        cStartEle.m_eElementType = EGameBoardType.Start;
        GameBoardElement cCurrentSquare = cStartEle;

        //Dirty method of finding path, possible infinite loop danger
        for (int iCurrentStep = 0; iCurrentStep < m_iOptimalStepCount; iCurrentStep++)
        {
            cCurrentSquare = RandomiseNextSquare(cCurrentSquare);            

            if (cCurrentSquare == null)
            {
                ResetPath();
                iCurrentStep = 0;
                cCurrentSquare = cStartEle;
            }
            else
            {
                cCurrentSquare.m_bIsPath = true;
            }
        }

        cCurrentSquare.m_eElementType = EGameBoardType.Finish;
    }

    /// <summary>
    /// If the path creation fails, reset the path
    /// </summary>
    void ResetPath()
    {
        for (int i = 0; i < m_iBoardWidth; i++)
        {
            for (int j = 0; j < m_iBoardHeight; j++)
            {
                m_acGameBoardData[i, j].m_bIsPath = false;
            }
        }
    }

    /// <summary>
    /// Gets a valid random square
    /// </summary>
    /// <param name="_cEle"></param>
    /// <returns></returns>
    GameBoardElement RandomiseNextSquare(GameBoardElement _cEle)
    {
        List<GameBoardElement> lValidSquares = new List<GameBoardElement>();

        if (_cEle.m_cTopElement != null)
        {
            if (!_cEle.m_cTopElement.m_bIsPath && _cEle.m_cTopElement.m_eElementType == EGameBoardType.Empty)
            {
                lValidSquares.Add(_cEle.m_cTopElement);
            }
        }

        if (_cEle.m_cBotElement != null)
        {
            if (!_cEle.m_cBotElement.m_bIsPath && _cEle.m_cBotElement.m_eElementType == EGameBoardType.Empty)
            {
                lValidSquares.Add(_cEle.m_cBotElement);
            }
        }

        if (_cEle.m_cLeftElement != null)
        {
            if (!_cEle.m_cLeftElement.m_bIsPath && _cEle.m_cLeftElement.m_eElementType == EGameBoardType.Empty)
            {
                lValidSquares.Add(_cEle.m_cLeftElement);
            }
        }

        if (_cEle.m_cRightElement != null)
        {
            if (!_cEle.m_cRightElement.m_bIsPath && _cEle.m_cRightElement.m_eElementType == EGameBoardType.Empty)
            {
                lValidSquares.Add(_cEle.m_cRightElement);
            }
        }

        if(lValidSquares.Count > 0)
        {
            return lValidSquares[Random.Range(0, lValidSquares.Count - 1)];
        }

        return null;
    }

    /// <summary>
    /// Places enemies in empty spaces
    /// </summary>
    void CreateEnemies()
    {
        for(int iCurrentEnemies = 0; iCurrentEnemies < m_iNumberOfEnemies; )
        {
            int iRandomEnemyX = Random.Range(0, m_iBoardWidth);
            int iRandomEnemyY = Random.Range(0, m_iBoardHeight);

            GameBoardElement cPossibleEnemyPlacement = m_acGameBoardData[iRandomEnemyX, iRandomEnemyY];

            if (!cPossibleEnemyPlacement.m_bIsPath
             && cPossibleEnemyPlacement.m_eElementType == EGameBoardType.Empty)
            {
                cPossibleEnemyPlacement.m_eElementType = EGameBoardType.Enemy;
                iCurrentEnemies++;
            }
        }
    }

    /// <summary>
    /// Renders Board
    /// </summary>
    void FullGameBoardRender()
    {
        //Create example of square to find width and height ONCE per full render!!!
        GameObject gTestSquare = Instantiate(m_gSquareBackground, transform);

        m_fSquareWidth = gTestSquare.GetComponent<RectTransform>().sizeDelta.x;
        m_fSquareHeight = gTestSquare.GetComponent<RectTransform>().sizeDelta.y;

        Destroy(gTestSquare);
        ////////////////////////////////////////////////////

        //Renderesthe board using supplied prefabs
        for (int i = 0; i < m_iBoardWidth; i++)
        {
            for (int j = 0; j < m_iBoardHeight; j++)
            {
                GameObject gNewSquare;
                EGameBoardType eType = m_acGameBoardData[i, j].m_eElementType;

                switch (eType)
                {
                    case EGameBoardType.Start:
                        gNewSquare = Instantiate(m_gStartObject, transform);
                        m_gActivePlayer = Instantiate(m_gPlayerObject, transform);
                        break;

                    case EGameBoardType.Finish:
                        gNewSquare = Instantiate(m_gFinishObject, transform);
                        break;

                    case EGameBoardType.Enemy:
                        gNewSquare = Instantiate(m_gEnemyObject, transform);
                        break;

                    default:
                        gNewSquare = Instantiate(m_gSquareBackground, transform);
                        break;
                }

                gNewSquare.name += "[" + i.ToString() + "," + j.ToString() + "]";
                m_acGameBoardData[i, j].m_gConnectedGameObject = gNewSquare;
                RectTransform gNewSquRect = gNewSquare.GetComponent<RectTransform>();

                float fScreenCenterOffsetX = (m_fSquareWidth * m_iBoardWidth) * 0.5f;
                float fScreenCenterOffsetY = (m_fSquareHeight * m_iBoardHeight) * 0.5f;

                gNewSquRect.localPosition = new Vector3( (j * m_fSquareHeight) - fScreenCenterOffsetY, (i * -m_fSquareWidth) + fScreenCenterOffsetX, 0.0f );

                if(eType == EGameBoardType.Start)
                {
                    m_gActivePlayer.GetComponent<RectTransform>().localPosition = new Vector3(((j * m_fSquareHeight) + (m_fSquareHeight * 0.5f)) - fScreenCenterOffsetY, ((i * -m_fSquareWidth) - (m_fSquareWidth * 0.5f)) + fScreenCenterOffsetX, 0.0f);
                    m_gActivePlayer.GetComponent<Player>().m_cSquareBelowPlayer = m_acGameBoardData[i, j];
                    m_cPlayerScript = m_gActivePlayer.GetComponent<Player>();
                }
            }
        }
    }
}

/// <summary>
/// Stores infomation for the individual board square
/// </summary>
public class GameBoardElement
{
    /// <summary>
    /// Stores references to the 4 elements around itself to remove the need to iterate through an array
    /// </summary>
    public GameBoardElement m_cLeftElement;
    public GameBoardElement m_cRightElement;
    public GameBoardElement m_cTopElement;
    public GameBoardElement m_cBotElement;

    public EGameBoardType m_eElementType;

    public bool m_bIsPath = false;

    /// <summary>
    /// Makes getting the index easy while protecting it from incorrect setting
    /// </summary>
    public int m_iIndexX
    {
        get;
        private set;
    }

    public int m_iIndexY
    {
        get;
        private set;
    }

    public GameObject m_gConnectedGameObject
    {
        get;
        set;
    }

    public GameBoardElement(int _iIndexX, int _iIndexY)
    {
        m_iIndexX = _iIndexX;
        m_iIndexY = _iIndexY;
    }
}
