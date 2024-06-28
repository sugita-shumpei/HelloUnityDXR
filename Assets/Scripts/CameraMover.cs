using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SocialPlatforms;

public class CameraMover : MonoBehaviour
{
    // WASD�F�O�㍶�E�̈ړ�
    // QE�F�㏸�E�~��
    // �E�h���b�O�F�J�����̉�]
    // ���h���b�O�F�O�㍶�E�̈ړ�
    // �X�y�[�X�F�J��������̗L���E�����̐؂�ւ�
    // P�F��]�����s���̏�Ԃɏ���������

    //�J�����̈ړ���
    [SerializeField, Range(0.1f, 20.0f)]
    private float _positionStep = 20.0f;

    //�}�E�X���x
    [SerializeField, Range(30.0f, 300.0f)]
    private float _mouseSensitive = 150.0f;

    // �J�����ړ��̑��x
    [SerializeField, Range(0.1f, 10.0f)]
    private float speed = 2.0f;

    //�J��������̗L������
    public static bool _cameraMoveActive = true;
    //�J������transform  
    private Transform _camTransform;
    //�}�E�X�̎n�_ 
    private Vector3 _startMousePos;
    //�J������]�̎n�_���
    private Vector3 _presentCamRotation;
    private Vector3 _presentCamPos;
    //������� Rotation
    private Quaternion _initialCamRotation;
    //UI���b�Z�[�W�̕\��
    private bool _uiMessageActiv;

    private float scroll;

    void Start()
    {
        _camTransform = this.gameObject.transform;

        //������]�̕ۑ�
        _initialCamRotation = this.gameObject.transform.rotation;
    }

    void Update()
    {
        PointerEventData pointer = new PointerEventData(EventSystem.current);
        pointer.position = Input.mousePosition;

        CamControlIsActive(); //�J��������̗L������

        if (_cameraMoveActive)
        {
            ResetCameraRotation(); //��]�p�x�̂݃��Z�b�g
            CameraRotationMouseControl(); //�J�����̉�] �}�E�X
            CameraSlideMouseControl(); //�J�����̏c���ړ� �}�E�X
            CameraPositionKeyControl(); //�J�����̃��[�J���ړ� �L�[
            CameraDolly();              //�z�C�[���ł̃h���[�C���E�A�E�g
        }
    }

    //�J��������̗L������
    public void CamControlIsActive()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _cameraMoveActive = !_cameraMoveActive;

            if (_uiMessageActiv == false)
            {
                StartCoroutine(DisplayUiMessage());
            }
            //Debug.Log("CamControl : " + _cameraMoveActive);
        }
    }

    //��]��������Ԃɂ���
    private void ResetCameraRotation()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            this.gameObject.transform.rotation = _initialCamRotation;
            //Debug.Log("Cam Rotate : " + _initialCamRotation.ToString());
        }
    }

    //�J�����̉�] �}�E�X
    private void CameraRotationMouseControl()
    {
        if (Input.GetMouseButtonDown(0) && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
        {
            _startMousePos = Input.mousePosition;
            _presentCamRotation.x = _camTransform.transform.eulerAngles.x;
            _presentCamRotation.y = _camTransform.transform.eulerAngles.y;

            _presentCamPos = _camTransform.position;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            _startMousePos = Input.mousePosition;
            _presentCamPos = _camTransform.position;
        }

        if (Input.GetMouseButton(0) && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
        {
            float x = (_startMousePos.x - Input.mousePosition.x) / Screen.width;
            float y = (_startMousePos.y - Input.mousePosition.y) / Screen.height;

            //��]�J�n�p�x �{ �}�E�X�̕ω��� * �}�E�X���x
            float eulerX = _presentCamRotation.x + y * _mouseSensitive;
            float eulerY = _presentCamRotation.y + x * _mouseSensitive;

            //_camTransform.position = new Vector3(_presentCamPos.x * Mathf.Cos(eulerX) + _presentCamPos.z * Mathf.Sin(eulerX),
            //                                     _presentCamPos.y,
            //                                   -_presentCamPos.x * Mathf.Sin(eulerX) + _presentCamPos.z * Mathf.Sin(eulerX)
            //                                     );
            _camTransform.rotation = Quaternion.Euler(eulerX, eulerY, 0);
        }
        else if (Input.GetMouseButton(0) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            //(�ړ��J�n���W - �}�E�X�̌��ݍ��W) / �𑜓x �Ő��K��
            float x = (_startMousePos.x - Input.mousePosition.x) / Screen.width;
            float y = (_startMousePos.y - Input.mousePosition.y) / Screen.height;

            x = x * _positionStep;
            y = y * _positionStep;

            Vector3 velocity = _camTransform.rotation * new Vector3(x, y, 0);
            velocity = velocity + _presentCamPos;
            _camTransform.position = velocity;
        }
    }

    //�J�����̈ړ� �}�E�X
    private void CameraSlideMouseControl()
    {
        if (Input.GetMouseButtonDown(2))
        {
            _startMousePos = Input.mousePosition;
            _presentCamPos = _camTransform.position;
        }

        if (Input.GetMouseButton(2))
        {
            //(�ړ��J�n���W - �}�E�X�̌��ݍ��W) / �𑜓x �Ő��K��
            float x = (_startMousePos.x - Input.mousePosition.x) / Screen.width;
            float y = (_startMousePos.y - Input.mousePosition.y) / Screen.height;

            x = x * _positionStep;
            y = y * _positionStep;

            Vector3 velocity = _camTransform.rotation * new Vector3(x, y, 0);
            velocity = velocity + _presentCamPos;
            _camTransform.position = velocity;
        }
    }

    //�J�����̃��[�J���ړ� �L�[
    private void CameraPositionKeyControl()
    {
        Vector3 campos = _camTransform.position;

        if (Input.GetKey(KeyCode.D)) { campos += _camTransform.right * Time.deltaTime * _positionStep; }
        if (Input.GetKey(KeyCode.A)) { campos -= _camTransform.right * Time.deltaTime * _positionStep; }
        if (Input.GetKey(KeyCode.E)) { campos += _camTransform.up * Time.deltaTime * _positionStep; }
        if (Input.GetKey(KeyCode.Q)) { campos -= _camTransform.up * Time.deltaTime * _positionStep; }
        if (Input.GetKey(KeyCode.W)) { campos += _camTransform.forward * Time.deltaTime * _positionStep; }
        if (Input.GetKey(KeyCode.S)) { campos -= _camTransform.forward * Time.deltaTime * _positionStep; }

        _camTransform.position = campos;
    }

    private void CameraDolly()
    {
        // �}�E�X�z�C�[���̉�]�l��ϐ� scroll �ɓn��
        scroll = Input.GetAxis("Mouse ScrollWheel");

        // �J�����̑O��ړ�����
        //�i�J�����������Ă������ forward �ɕϐ� scroll �� speed ����Z���ĉ��Z����j
        this.gameObject.transform.position += transform.forward * scroll * speed;
    }

    //UI���b�Z�[�W�̕\��
    private IEnumerator DisplayUiMessage()
    {
        _uiMessageActiv = true;
        float time = 0;
        while (time < 2)
        {
            time = time + Time.deltaTime;
            yield return null;
        }
        _uiMessageActiv = false;
    }

    /*void OnGUI()
    {
        if (_uiMessageActiv == false) { return; }
        GUI.color = Color.black;
        if (_cameraMoveActive == true)
        {
            GUI.Label(new Rect(Screen.width / 2 - 50, Screen.height - 30, 100, 20), "�J�������� �L��");
        }

        if (_cameraMoveActive == false)
        {
            GUI.Label(new Rect(Screen.width / 2 - 50, Screen.height - 30, 100, 20), "�J�������� ����");
        }
    }*/

}
