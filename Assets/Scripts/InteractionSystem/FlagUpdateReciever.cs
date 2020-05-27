using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface FlagUpdateReciever
{
    void flag_set(string flag);
    void flag_unset(string flag);
}
