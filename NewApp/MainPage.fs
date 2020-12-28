namespace NewApp

open Avalonia.FuncUI.DSL

open Avalonia.Layout
open Avalonia.Controls

module MainPage =
    open Elmish

    type Model =
        { counterState: Counter.State
          bottom: userInfo.State }

    type Msg =
        | Reset
        | CounterMsg of Counter.Msg
        | UserMsg of userInfo.Msg
    // | Top of Counter.Msg
    // | Bottom of userInfo.Msg
    let init () =
        let top = Counter.init
        let bottom = userInfo.init ()
        { counterState = top; bottom = bottom }, Cmd.batch []


    let update (msg: Msg) (state: Model): Model * Cmd<_> =
        match msg with
        | CounterMsg x ->
            let newState = Counter.update x state.counterState
            { state with counterState = newState }, Cmd.none


    let view (state: Model) (dispatch) =
        DockPanel.create [ DockPanel.children [ userInfo.view state.bottom (UserMsg >> dispatch)
                                                Counter.view state.counterState (CounterMsg >> dispatch) ] ]
